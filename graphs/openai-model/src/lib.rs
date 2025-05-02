use graphs_ai::model::{ChatCompletionResponse, ModelClient};
use log::{debug, info};
use serde_json::Value;

/// A model client for the `OpenAI` API.
pub struct OpenAIModel {
    api_key: String,
    model: String,
    base_url: String,
}

impl OpenAIModel {
    pub fn new(
        api_key: impl Into<String>,
        model: impl Into<String>,
        base_url: impl Into<String>,
    ) -> Self {
        Self {
            api_key: api_key.into(),
            model: model.into(),
            base_url: base_url.into(),
        }
    }
}

impl ModelClient for OpenAIModel {
    fn get_model_response(
        &self,
        request: &graphs_ai::model::ChatCompletionRequest,
    ) -> graphs_ai::model::ChatCompletionResponse {
        // let request: OpenAIChatCompletionRequest = convert_request(request, &self.model);

        let client = reqwest::blocking::Client::new();

        let url = format!("{}/chat/completions", self.base_url);

        info!(
            "Sending request to llm api at {} with model {}",
            url, self.model
        );

        let body = serde_json::to_string(&request).expect("Failed to serialize request body");

        if log::log_enabled!(log::Level::Debug) {
            debug!("Request body: {body}");
        }

        let response = client
            .post(url)
            .header("Authorization", format!("Bearer {}", self.api_key))
            .header("Content-Type", "application/json")
            .json(&request)
            .send()
            .expect("Failed to send request to OpenAI");

        let response_json: Value = response.json().expect("Failed to parse OpenAI response");

        debug!("Response: {response_json}");

        let openai_response = serde_json::from_value::<ChatCompletionResponse>(response_json)
            .expect("Failed to parse OpenAI response");

        debug!("parsed openai response: {openai_response:?}");

        openai_response
    }
}
