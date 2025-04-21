use crate::model::ModelClient;

pub struct OpenAIModel {
    api_key: String,
    model: String,
}

impl OpenAIModel {
    pub fn new(api_key: String, model: String) -> Self {
        Self { api_key, model }
    }
}

impl ModelClient for OpenAIModel {
    fn get_model_response(
        &self,
        request: &crate::model::ChatCompletionRequest,
    ) -> crate::model::ChatCompletionResponse {
    }
}
