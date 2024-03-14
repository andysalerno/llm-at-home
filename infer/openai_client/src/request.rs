use libinfer::generate_request::GenerateRequest;
use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize)]
pub(crate) struct Message {
    role: String,
    content: String,
}

#[derive(Debug, Serialize, Deserialize)]
pub(crate) struct CompletionsRequest {
    model: String,
    prompt: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    max_tokens: Option<i32>,
    #[serde(skip_serializing_if = "Option::is_none")]
    temperature: Option<f32>,
    #[serde(skip_serializing_if = "Option::is_none")]
    top_p: Option<f32>,
    stream: bool,
    #[serde(skip_serializing_if = "Option::is_none")]
    stop: Option<Vec<String>>,
    #[serde(skip_serializing_if = "Option::is_none")]
    presence_penalty: Option<f32>,
    #[serde(skip_serializing_if = "Option::is_none")]
    frequency_penalty: Option<f32>,
    #[serde(skip_serializing_if = "Option::is_none")]
    repetition_penalty: Option<f32>,
    #[serde(skip_serializing_if = "Option::is_none")]
    min_p: Option<f32>,
    #[serde(skip_serializing_if = "Option::is_none")]
    top_k: Option<i32>,
    #[serde(skip_serializing_if = "Option::is_none")]
    length_penalty: Option<usize>,
    guided_json: Option<String>,
}

#[allow(unused)]
impl CompletionsRequest {
    pub(crate) fn model(&self) -> &str {
        &self.model
    }

    pub(crate) fn prompt(&self) -> &str {
        &self.prompt
    }

    pub(crate) fn max_tokens(&self) -> Option<i32> {
        self.max_tokens
    }

    pub(crate) fn temperature(&self) -> Option<f32> {
        self.temperature
    }

    pub(crate) fn top_p(&self) -> Option<f32> {
        self.top_p
    }

    pub(crate) fn stream(&self) -> bool {
        self.stream
    }

    pub(crate) fn stop(&self) -> Option<&Vec<String>> {
        self.stop.as_ref()
    }

    pub(crate) fn presence_penalty(&self) -> Option<f32> {
        self.presence_penalty
    }

    pub(crate) fn frequency_penalty(&self) -> Option<f32> {
        self.frequency_penalty
    }

    pub(crate) fn repetition_penalty(&self) -> Option<f32> {
        self.repetition_penalty
    }

    pub(crate) fn min_p(&self) -> Option<f32> {
        self.min_p
    }

    pub(crate) fn top_k(&self) -> Option<i32> {
        self.top_k
    }

    pub(crate) fn length_penalty(&self) -> Option<usize> {
        self.length_penalty
    }

    pub(crate) fn guided_json(&self) -> Option<&String> {
        self.guided_json.as_ref()
    }
}

impl From<&GenerateRequest> for CompletionsRequest {
    fn from(value: &GenerateRequest) -> Self {
        CompletionsRequest {
            model: "model".into(),
            prompt: value.inputs().into(),
            max_tokens: value.parameters().max_new_tokens,
            temperature: value.parameters().temperature,
            top_p: value.parameters().top_p,
            top_k: value.parameters().top_k,
            guided_json: None,
            length_penalty: None,
            min_p: None,
            presence_penalty: None,
            repetition_penalty: value.parameters().repetition_penalty,
            stop: value.parameters().stop.clone(),
            stream: true,
            frequency_penalty: None,
        }
    }
}
