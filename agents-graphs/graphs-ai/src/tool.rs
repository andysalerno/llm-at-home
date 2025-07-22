use std::fmt::Debug;

use log::info;
use schemars::{JsonSchema, schema::SchemaObject, schema_for};
use serde::{Deserialize, Serialize};

use crate::model;

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct ToolDescription {
    pub name: String,
    pub description: String,
    pub parameters: ToolSchema,
}

pub trait Tool {
    fn json_schema(&self) -> &ToolSchema;
    fn name(&self) -> &str;
    fn description(&self) -> &str;
    fn get_output(&self, input_json: &str) -> String;

    fn get_full_description(&self) -> ToolDescription {
        ToolDescription {
            name: self.name().to_string(),
            description: self.description().to_string(),
            parameters: self.json_schema().clone(),
        }
    }
}

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct ToolSchema(SchemaObject);

impl ToolSchema {
    pub fn generate_schema<T: JsonSchema>() -> Self {
        Self(schema_for!(T).schema)
    }

    pub fn from_schema_str(schema: &str) -> Self {
        info!("parsing schema: {schema}");
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

// impl From<&dyn Tool> for model::Tool {
//     fn from(tool: &dyn Tool) -> Self {
//         Self::new(
//             tool.name(),
//             tool.description(),
//             tool.json_schema().clone(),
//             true,
//         )
//     }
// }

impl From<ToolDescription> for model::Tool {
    fn from(tool: ToolDescription) -> Self {
        Self::new(tool.name, tool.description, tool.parameters, true)
    }
}
