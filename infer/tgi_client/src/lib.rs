//! A client for the text-generation-inference service that implements `LLMClient`.
mod info;

use async_trait::async_trait;
use info::Info;
use libinfer::{
    embeddings::{EmbeddingsRequest, EmbeddingsResponse},
    generate_request::GenerateRequest,
    llm_client::{InferenceStream, LLMClient, StreamEvent},
};
use log::{debug, info};
use reqwest::{Client, IntoUrl, Url};
use reqwest_eventsource::EventSource;

/// A client that is compatible with text-generation-inference and implements `LLMClient`.
pub struct TgiClient {
    inference_endpoint: Url,
}

impl TgiClient {
    /// Create a new `TgiClient`.
    /// # Panics
    /// Panics if the provided endpoint is not well formed.
    pub fn new(inference_endpoint: impl IntoUrl) -> Self {
        Self {
            inference_endpoint: inference_endpoint.into_url().expect("expected a valid url"),
        }
    }
}

#[async_trait]
impl LLMClient for TgiClient {
    fn get_response_stream(&self, request: &GenerateRequest) -> InferenceStream {
        let url = {
            let mut url = self.inference_endpoint.clone();
            url.set_path("generate_stream");
            url
        };

        debug!("Sending request:\n'{}'", request.inputs());

        let request = Client::new().post(url).json(request);

        let event_stream = EventSource::new(request).expect("Could not create EventSource");

        InferenceStream::new(event_stream, Box::new(mapper))
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
            url.set_path("info");
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

fn mapper(value: reqwest_eventsource::Event) -> StreamEvent {
    match value {
        reqwest_eventsource::Event::Open => StreamEvent::Open,
        reqwest_eventsource::Event::Message(event) => {
            let data = &event.data;
            debug!("saw data: '{data}'");
            StreamEvent::Message(serde_json::from_str(&event.data).unwrap_or_else(|_| {
                panic!(
                    "expected valid json in the response, but saw: '{}'",
                    &event.data
                )
            }))
        }
    }
}
