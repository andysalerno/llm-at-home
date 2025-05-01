pub mod mcp_tool;

use anyhow::Result;
use rmcp::{
    RoleClient, ServiceExt,
    model::{
        CallToolRequestParam, ClientCapabilities, ClientInfo, Implementation,
        InitializeRequestParam, ProtocolVersion, Tool,
    },
    service::RunningService,
    transport::SseTransport,
};
use std::sync::LazyLock;

static TOKIO_RT: LazyLock<tokio::runtime::Runtime> = LazyLock::new(|| {
    tokio::runtime::Builder::new_multi_thread() // or new_current_thread()
        .enable_all() // I/O, time, etc.
        .build()
        .expect("failed to build Tokio runtime")
});

pub struct McpContext {
    client: RunningService<RoleClient, InitializeRequestParam>,
}

impl McpContext {
    pub fn connect(url: impl reqwest::IntoUrl) -> Result<Self> {
        let client = TOKIO_RT.block_on(async { Self::connect_async(url).await })?;

        Ok(Self { client })
    }

    pub fn list_tools(&self) -> Result<Vec<Tool>> {
        TOKIO_RT.block_on(async {
            let tools = self.client.list_all_tools().await?;
            Ok(tools)
        })
    }

    pub fn call_tool(&self, input_json: &str) -> Result<String> {
        let result = TOKIO_RT.block_on(async {
            self.client
                .call_tool(CallToolRequestParam {
                    name: "name".into(),
                    arguments: None,
                })
                .await
        })?;

        let text = match result.content.first().unwrap().raw {
            rmcp::model::RawContent::Text(ref t) => t,
            _ => panic!("unexpected content"),
        };

        Ok(text.text.clone())
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
