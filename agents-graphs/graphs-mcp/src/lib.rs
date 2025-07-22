pub mod mcp_tool;

use anyhow::Result;
use mcp_tool::McpTool;
use rmcp::{
    RoleClient, ServiceExt,
    model::{
        CallToolRequestParam, ClientCapabilities, ClientInfo, Implementation,
        InitializeRequestParam, ProtocolVersion, Tool,
    },
    service::RunningService,
    transport::SseTransport,
};
use std::sync::{Arc, LazyLock};

static TOKIO_RT: LazyLock<tokio::runtime::Runtime> = LazyLock::new(|| {
    tokio::runtime::Builder::new_multi_thread() // or new_current_thread()
        .enable_all() // I/O, time, etc.
        .build()
        .expect("failed to build Tokio runtime")
});

pub struct McpContext {
    client: Arc<RunningService<RoleClient, InitializeRequestParam>>,
}

impl McpContext {
    pub fn connect(url: impl reqwest::IntoUrl) -> Result<Self> {
        let client = TOKIO_RT.block_on(async { Self::connect_async(url).await })?;

        Ok(Self {
            client: Arc::new(client),
        })
    }

    pub fn get_tools(&self) -> Result<Vec<Box<dyn graphs_ai::tool::Tool>>> {
        let tools = self.list_tools()?;

        let mut converted_tools: Vec<Box<dyn graphs_ai::tool::Tool>> = Vec::new();

        for tool in tools {
            let name = tool.name;
            let description = tool.description;
            let schema = tool.input_schema;

            let converted = McpTool::new(
                name.into(),
                description.into(),
                serde_json::to_string(schema.as_ref())?,
                McpContext {
                    client: Arc::clone(&self.client),
                },
            );

            converted_tools.push(Box::new(converted));
        }

        Ok(converted_tools)
    }

    pub fn call_tool(&self, tool_name: &str, input_json: &str) -> Result<String> {
        let json: serde_json::Value = serde_json::from_str(input_json)?;
        let json = json.as_object().unwrap().to_owned();

        let result = TOKIO_RT.block_on(async {
            self.client
                .call_tool(CallToolRequestParam {
                    name: tool_name.to_owned().into(),
                    arguments: Some(json),
                })
                .await
        })?;

        let text = match result.content.first().unwrap().raw {
            rmcp::model::RawContent::Text(ref t) => t,
            _ => panic!("unexpected content"),
        };

        Ok(text.text.clone())
    }

    fn list_tools(&self) -> Result<Vec<Tool>> {
        TOKIO_RT.block_on(async {
            let tools = self.client.list_all_tools().await?;
            Ok(tools)
        })
    }

    async fn connect_async(
        url: impl reqwest::IntoUrl,
    ) -> Result<RunningService<RoleClient, InitializeRequestParam>> {
        let transport = SseTransport::start(url).await?;
        let client_info = ClientInfo {
            protocol_version: ProtocolVersion::default(),
            capabilities: ClientCapabilities::default(),
            client_info: Implementation {
                name: "test sse client".to_string(),
                version: "0.0.1".to_string(),
            },
        };
        let client = client_info.serve(transport).await.inspect_err(|e| {
            tracing::error!("client error: {:?}", e);
        })?;

        // Initialize
        let server_info = client.peer_info();
        tracing::info!("Connected to server: {server_info:#?}");

        Ok(client)
    }
}
