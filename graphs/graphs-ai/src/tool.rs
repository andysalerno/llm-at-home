use schemars::{JsonSchema, schema::SchemaObject, schema_for};
use serde::{Deserialize, Serialize};

use crate::model;

pub struct ToolDescription {
    pub name: String,
    pub description: String,
    pub input_schema: ToolSchema,
}

pub trait Tool {
    fn json_schema(&self) -> &ToolSchema;
    fn name(&self) -> &str;
    fn description(&self) -> &str;
    fn get_output(&self, input_json: &str) -> String;
}

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct ToolSchema(SchemaObject);

impl ToolSchema {
    pub fn generate_schema<T: JsonSchema>() -> Self {
        Self(schema_for!(T).schema)
    }
}

impl From<&dyn Tool> for model::Tool {
    fn from(tool: &dyn Tool) -> Self {
        Self::new(
            tool.name(),
            tool.description(),
            tool.json_schema().clone(),
            false,
        )
    }
}
