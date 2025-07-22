use graphs_ai::tool::{Tool, ToolSchema};
use log::info;
use schemars::{JsonSchema, schema::RootSchema, schema_for};

pub struct WeatherTool {
    input_schema: ToolSchema,
}

impl WeatherTool {
    pub fn new() -> Self {
        WeatherTool {
            input_schema: ToolSchema::generate_schema::<WeatherToolParameters>(),
        }
    }
}

#[derive(JsonSchema)]
#[serde(deny_unknown_fields)]
pub struct WeatherToolParameters {
    /// The city for which to get the weather
    pub city: String,

    /// The unit of measurement (e.g., Celsius, Fahrenheit)
    pub unit: String,
}

impl Tool for WeatherTool {
    fn json_schema(&self) -> &ToolSchema {
        &self.input_schema
    }

    fn name(&self) -> &str {
        "weather_tool"
    }

    fn description(&self) -> &str {
        "gets the weather for a given city"
    }

    fn get_output(&self, input_json: &str) -> String {
        info!("WeatherTool input JSON: {input_json}");
        "it's 20 celcius and raining".into()
    }
}
