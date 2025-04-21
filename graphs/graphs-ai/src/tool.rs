pub trait Tool {
    fn json_schema(&self) -> String;
    fn name(&self) -> String;
    fn description(&self) -> String;
    fn get_output(&self, input_json: String) -> String;
}
