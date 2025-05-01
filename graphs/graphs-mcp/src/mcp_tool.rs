use graphs_ai::tool::Tool;

use crate::McpContext;

pub struct McpTool {
    context: McpContext,
}

impl Tool for McpTool {
    fn json_schema(&self) -> &graphs_ai::tool::ToolSchema {
        todo!()
    }

    fn name(&self) -> &str {
        todo!()
    }

    fn description(&self) -> &str {
        todo!()
    }

    fn get_output(&self, input_json: &str) -> String {
        self.context.call_tool(input_json).unwrap()
    }
}

pub fn mcp_tool() -> McpTool {
    todo!()
}
