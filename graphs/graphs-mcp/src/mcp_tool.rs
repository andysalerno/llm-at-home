use crate::McpContext;
use graphs_ai::tool::{self, Tool};
use schemars::JsonSchema;
use serde_json::Value;
use tracing::info;

pub struct McpTool {
    name: String,
    description: String,
    input_schema: String,
    tool_schema: graphs_ai::tool::ToolSchema,
    context: McpContext,
}

impl McpTool {
    pub fn new(
        name: String,
        description: String,
        input_schema: String,
        context: McpContext,
    ) -> Self {
        let tool_schema = Self::get_tool_schema(&input_schema);

        Self {
            name,
            description,
            input_schema,
            tool_schema,
            context,
        }
    }

    fn get_tool_schema(input_schema: &str) -> graphs_ai::tool::ToolSchema {
        let schema: graphs_ai::tool::ToolSchema = serde_json::from_str(input_schema).unwrap();
        schema
    }
}

impl Tool for McpTool {
    fn json_schema(&self) -> &graphs_ai::tool::ToolSchema {
        &self.tool_schema
    }

    fn name(&self) -> &str {
        &self.name
    }

    fn description(&self) -> &str {
        &self.description
    }

    fn get_output(&self, input_json: &str) -> String {
        self.context.call_tool(&self.name, input_json).unwrap()
    }
}

pub fn mcp_tool() -> McpTool {
    todo!()
}
