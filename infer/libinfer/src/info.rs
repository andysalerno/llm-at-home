use serde::{Deserialize, Serialize};

/// Results for a request to the info endpoint.
#[derive(Debug, Serialize, Deserialize)]
pub struct Info {
    model_id: String,
}

#[allow(missing_docs)]
impl Info {
    pub fn new(model_id: String) -> Self {
        Self { model_id }
    }

    pub fn model_id(&self) -> &str {
        &self.model_id
    }
}
