use serde::{Deserialize, Serialize};

/// Results for a request to the info endpoint.
#[derive(Debug, Serialize, Deserialize)]
pub struct Info {
    model_id: String,
    model_sha: Option<String>,
    model_dtype: String,
    model_device_type: String,
    model_pipeline_tag: Option<String>,
    max_concurrent_requests: u32,
    max_best_of: u32,
    max_stop_sequences: u32,
    max_input_length: u32,
    max_total_tokens: u32,
    waiting_served_ratio: f32,
    max_batch_total_tokens: u32,
    max_waiting_tokens: u32,
    validation_workers: u32,
    version: String,
    sha: String,
    docker_label: String,
}

#[allow(missing_docs)]
impl Info {
    #[must_use]
    pub fn model_id(&self) -> &str {
        self.model_id.as_ref()
    }

    #[must_use]
    pub fn model_sha(&self) -> Option<&str> {
        self.model_sha.as_deref()
    }

    #[must_use]
    pub fn model_dtype(&self) -> &str {
        self.model_dtype.as_ref()
    }

    #[must_use]
    pub fn model_device_type(&self) -> &str {
        self.model_device_type.as_ref()
    }

    #[must_use]
    pub fn model_pipeline_tag(&self) -> Option<&str> {
        self.model_pipeline_tag.as_deref()
    }

    #[must_use]
    pub fn max_concurrent_requests(&self) -> u32 {
        self.max_concurrent_requests
    }

    #[must_use]
    pub fn max_best_of(&self) -> u32 {
        self.max_best_of
    }

    #[must_use]
    pub fn max_stop_sequences(&self) -> u32 {
        self.max_stop_sequences
    }

    #[must_use]
    pub fn max_input_length(&self) -> u32 {
        self.max_input_length
    }

    #[must_use]
    pub fn max_total_tokens(&self) -> u32 {
        self.max_total_tokens
    }

    #[must_use]
    pub fn waiting_served_ratio(&self) -> f32 {
        self.waiting_served_ratio
    }

    #[must_use]
    pub fn max_batch_total_tokens(&self) -> u32 {
        self.max_batch_total_tokens
    }

    #[must_use]
    pub fn max_waiting_tokens(&self) -> u32 {
        self.max_waiting_tokens
    }

    #[must_use]
    pub fn validation_workers(&self) -> u32 {
        self.validation_workers
    }

    #[must_use]
    pub fn version(&self) -> &str {
        self.version.as_ref()
    }

    #[must_use]
    pub fn sha(&self) -> &str {
        self.sha.as_ref()
    }

    #[must_use]
    pub fn docker_label(&self) -> &str {
        self.docker_label.as_ref()
    }
}
