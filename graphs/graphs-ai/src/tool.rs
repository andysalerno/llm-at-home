use std::fmt::Debug;

use schemars::{JsonSchema, schema::SchemaObject, schema_for};
use serde::{Deserialize, Serialize};

use crate::model;

pub trait Tool {
    fn json_schema(&self) -> &ToolSchema;
    fn name(&self) -> &str;
    fn description(&self) -> &str;
    fn get_output(&self, input_json: &str) -> String;
}

impl dyn Tool {
    pub fn to_model_tool(&self) -> model::Tool {
        self.into()
    }
}

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct ToolSchema(SchemaObject);

impl ToolSchema {
    pub fn generate_schema<T: JsonSchema>() -> Self {
        Self(schema_for!(T).schema)
    }

    pub fn from_schema_str(schema: &str) -> Self {
        let schema: Self = serde_json::from_str(schema).unwrap();
        schema
    }
}

impl Debug for dyn Tool {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        f.debug_struct("Tool")
            .field("name", &self.name())
            .field("description", &self.description())
            .field("json_schema", &self.json_schema())
            .finish()
    }
}

impl From<&dyn Tool> for model::Tool {
    fn from(tool: &dyn Tool) -> Self {
        Self::new(
            tool.name(),
            tool.description(),
            tool.json_schema().clone(),
            true,
        )
    }
}
