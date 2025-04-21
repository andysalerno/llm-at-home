use crate::model::ModelClient;
use serde_json::{Value, json};

pub struct OpenAIModel {
    api_key: String,
    model: String,
}

impl OpenAIModel {
    pub fn new(api_key: impl Into<String>, model: impl Into<String>) -> Self {
        Self {
            api_key: api_key.into(),
            model: model.into(),
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

        let response = client
            .post("https://api.openai.com/v1/chat/completions")
            .header("Authorization", format!("Bearer {}", self.api_key))
            .header("Content-Type", "application/json")
            .json(&json!({
                "model": self.model,
                "messages": messages,
                "temperature": request.temperature(),
            }))
            .send()
            .expect("Failed to send request to OpenAI");

        let response_json: Value = response.json().expect("Failed to parse OpenAI response");

        serde_json::from_value::<crate::model::ChatCompletionResponse>(response_json)
            .expect("Failed to parse OpenAI response")
    }
}
