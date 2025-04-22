use graphs_ai::tool::Tool;

pub struct SampleTool;

impl SampleTool {
    pub fn new() -> Self {
        SampleTool
    }
}

impl Tool for SampleTool {
    fn json_schema(&self) -> &str {
        "{}"
    }

    fn name(&self) -> &str {
        "SampleTool"
    }

    fn description(&self) -> &str {
        "A sample tool"
    }

    fn get_output(&self, input_json: &str) -> String {
        "sample tool output".into()
    }
}
