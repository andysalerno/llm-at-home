use crate::model::ModelClient;
use log::{debug, info};
use serde_json::{Value, json};

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
        request: &crate::model::ChatCompletionRequest,
    ) -> crate::model::ChatCompletionResponse {
        let client = reqwest::blocking::Client::new();

        let messages: Vec<Value> = request
            .messages()
            .iter()
            .map(|msg| {
                json!({
                    "role": msg.role(),
                    "content": msg.content()
                })
            })
            .collect();

        let url = format!("{}/chat/completions", self.base_url);

        info!(
            "Sending request to llm api at {} with model {}",
            url, self.model
        );

        let body = json!({
            "model": self.model,
            "messages": messages,
            "temperature": request.temperature(),
        });

        debug!("Request body: {body}");

        let response = client
            .post(url)
            .header("Authorization", format!("Bearer {}", self.api_key))
            .header("Content-Type", "application/json")
            .json(&body)
            .send()
            .expect("Failed to send request to OpenAI");

        let response_json: Value = response.json().expect("Failed to parse OpenAI response");

        serde_json::from_value::<crate::model::ChatCompletionResponse>(response_json)
            .expect("Failed to parse OpenAI response")
    }
}
