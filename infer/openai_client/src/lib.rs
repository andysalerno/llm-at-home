//! A client for the text-generation-inference service that implements `LLMClient`.

mod info;
mod request;

use async_trait::async_trait;
use info::Info;
use libinfer::{
    embeddings::{EmbeddingsRequest, EmbeddingsResponse},
    generate_request::GenerateRequest,
    llm_client::{InferenceStream, LLMClient},
};
use log::{debug, info};
use reqwest::{Client, IntoUrl, Url};
use reqwest_eventsource::EventSource;

use crate::request::CompletionsRequest;

/// A client that is compatible with text-generation-inference and implements `LLMClient`.
pub struct OpenAIClient {
    inference_endpoint: Url,
}

impl OpenAIClient {
    /// Create a new `OpenAIClient`.
    /// # Panics
    /// Panics if the provided endpoint is not well formed.
    pub fn new(inference_endpoint: impl IntoUrl) -> Self {
        Self {
            inference_endpoint: inference_endpoint.into_url().expect("expected a valid url"),
        }
    }
}

#[async_trait]
impl LLMClient for OpenAIClient {
    fn get_response_stream(&self, request: &GenerateRequest) -> InferenceStream {
        let url = {
            let mut url = self.inference_endpoint.clone();
            url.set_path("v1/completions");
            url
        };

        let request: CompletionsRequest = request.into();

        let as_json = serde_json::to_string_pretty(&request).unwrap();
        debug!("Sending request:\n'{}'", as_json);

        let request = Client::new().post(url).json(&request);

        debug!("Sending request:\n'{:?}'", request);

        let event_stream = EventSource::new(request).expect("Could not create EventSource");

        InferenceStream::new(event_stream)
    }

    async fn get_embeddings(&self, request: &EmbeddingsRequest) -> EmbeddingsResponse {
        let url = {
            let mut url = self.inference_endpoint.clone();
            let _ = url.set_port(Some(8000));
            url.set_path("embeddings");
            url
        };

        info!("Requesting embeddings...");
        let client = reqwest::Client::new();
        let response = client.post(url).json(request).send().await.unwrap();
        info!("...done.");

        let parsed: EmbeddingsResponse = response.json().await.unwrap();

        parsed
    }

    async fn get_info(&self) -> libinfer::info::Info {
        let url = {
            let mut url = self.inference_endpoint.clone();
            url.set_path("v1/models");
            url
        };

        let r = reqwest::get(url)
            .await
            .expect("Could not retrieve info from endpoint");

        r.json::<Info>()
            .await
            .expect("Response from /info was not parsable as expected")
            .into()
    }
}
