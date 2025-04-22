pub trait Tool {
    fn json_schema(&self) -> &str;
    fn name(&self) -> &str;
    fn description(&self) -> &str;
    fn get_output(&self, input_json: &str) -> String;
}
