use derive_builder::Builder;
use serde::{Deserialize, Serialize};

/// The parameters that can be provided when requesting text generation.
#[allow(missing_docs)]
#[derive(Debug, Serialize, Deserialize, Clone, Builder)]
pub struct GenerateParameters {
    #[serde(default, skip_serializing_if = "Option::is_none")]
    #[builder(default)]
    pub best_of: Option<i32>,

    #[serde(default = "default_decoder_input_details")]
    #[builder(default)]
    pub decoder_input_details: bool,

    #[serde(default = "default_details")]
    #[builder(default)]
    pub details: bool,

    #[serde(default, skip_serializing_if = "Option::is_none")]
    #[builder(default)]
    pub do_sample: Option<bool>,

    #[serde(default, skip_serializing_if = "Option::is_none")]
    #[builder(default = "Some(800)")]
    pub max_new_tokens: Option<i32>,

    #[serde(default, skip_serializing_if = "Option::is_none")]
    #[builder(default = "Some(1.0)")]
    pub repetition_penalty: Option<f32>,

    #[serde(default, skip_serializing_if = "Option::is_none")]
    #[builder(default)]
    pub return_full_text: Option<bool>,

    #[serde(default, skip_serializing_if = "Option::is_none")]
    #[builder(default)]
    pub seed: Option<i64>,

    #[serde(default, skip_serializing_if = "Option::is_none")]
    #[builder(default)]
    pub stop: Option<Vec<String>>,

    #[serde(default, skip_serializing_if = "Option::is_none")]
    #[builder(default = "Some(0.8)")]
    pub temperature: Option<f32>,

    #[serde(default, skip_serializing_if = "Option::is_none")]
    #[builder(default)]
    pub top_k: Option<i32>,

    #[serde(default, skip_serializing_if = "Option::is_none")]
    #[builder(default)]
    pub top_n_tokens: Option<i32>,

    #[serde(default, skip_serializing_if = "Option::is_none")]
    #[builder(default = "Some(0.3)")]
    pub top_p: Option<f32>,

    #[serde(default, skip_serializing_if = "Option::is_none")]
    #[builder(default)]
    pub truncate: Option<i32>,

    #[serde(default, skip_serializing_if = "Option::is_none")]
    #[builder(default)]
    pub typical_p: Option<f32>,

    #[serde(default = "default_watermark")]
    #[builder(default)]
    pub watermark: bool,
}

impl Default for GenerateParameters {
    fn default() -> Self {
        Self {
            decoder_input_details: default_decoder_input_details(),
            details: default_details(),
            watermark: default_watermark(),
            best_of: Option::default(),
            do_sample: Option::default(),
            max_new_tokens: Option::default(),
            repetition_penalty: Option::default(),
            return_full_text: Option::default(),
            seed: Option::default(),
            stop: Option::default(),
            temperature: Option::default(),
            top_k: Option::default(),
            top_n_tokens: Option::default(),
            top_p: Option::default(),
            truncate: Option::default(),
            typical_p: Option::default(),
        }
    }
}

fn default_decoder_input_details() -> bool {
    true
}

fn default_details() -> bool {
    true
}

fn default_watermark() -> bool {
    false
}

/// A request for text generation.
#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct GenerateRequest {
    inputs: String,
    parameters: GenerateParameters,
}

impl GenerateRequest {
    /// Create a new requet for text generation.
    #[must_use]
    pub fn new(inputs: String, parameters: GenerateParameters) -> Self {
        Self { inputs, parameters }
    }

    /// Gets the inputs..
    #[must_use]
    pub fn inputs(&self) -> &str {
        self.inputs.as_ref()
    }

    /// Gets the parameters.
    #[must_use]
    pub fn parameters(&self) -> &GenerateParameters {
        &self.parameters
    }
}
