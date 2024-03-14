use crate::embeddings::{EmbeddingsRequest, EmbeddingsResponse};
use crate::generate_request::GenerateRequest;
use crate::info::Info;
use async_trait::async_trait;
use futures_util::{Stream, StreamExt};
use log::{debug, error, info, warn};
use reqwest_eventsource::{Event, EventSource};
use serde::{Deserialize, Serialize};
use std::{pin::Pin, task::Poll};

/// A trait describing the core client interaction to a LLM service.
#[async_trait]
pub trait LLMClient {
    /// Get a response stream from the LLM.
    fn get_response_stream(&self, request: &GenerateRequest) -> InferenceStream;

    /// Get a embeddings from the LLM.
    async fn get_embeddings(&self, request: &EmbeddingsRequest) -> EmbeddingsResponse;

    /// Get info from the LLM about its current configuration.
    async fn get_info(&self) -> Info;

    /// Like get_response_stream, but blocks until the stream is complete and returns the result.
    async fn get_response(&self, request: &GenerateRequest) -> String {
        let mut stream = self.get_response_stream(request);

        let mut assistant_response = String::new();

        while let Some(event) = stream.next().await {
            match event {
                Ok(StreamEvent::Open) => debug!("connection open"),
                Ok(StreamEvent::Message(m)) => {
                    debug!("message received: {m:?}");
                    assistant_response.push_str(m.token().text());
                }
                Err(StreamError::StreamClose) => {
                    debug!("stream complete");
                    stream.close();
                }
                e => {
                    warn!("unknown event: {e:?}");
                    stream.close();
                }
            }
        }

        assistant_response
    }
}

/// A struct that represents streaming response.
pub struct InferenceStream {
    inner_stream: EventSource,
    mapper: Box<dyn (FnMut(Event) -> StreamEvent) + Send>,
}

impl InferenceStream {
    /// Create a new `InferenceStream`.
    pub fn new(
        inner_stream: EventSource,
        mapper: Box<dyn (FnMut(Event) -> StreamEvent) + Send>,
    ) -> Self {
        Self {
            inner_stream,
            mapper,
        }
    }
}

impl Stream for InferenceStream {
    type Item = Result<StreamEvent, StreamError>;

    fn poll_next(
        self: std::pin::Pin<&mut Self>,
        cx: &mut std::task::Context<'_>,
    ) -> std::task::Poll<Option<Self::Item>> {
        let this = self.get_mut();
        let mapper = &mut this.mapper;

        match Pin::new(&mut this.inner_stream).poll_next(cx) {
            Poll::Ready(Some(Ok(value))) => Poll::Ready(Some(Ok(mapper(value)))),
            Poll::Ready(Some(Err(e))) => {
                error!("error: '{e:?}'");
                this.inner_stream.close();
                Poll::Ready(Some(Err(e.into())))
            }
            Poll::Ready(None) => Poll::Ready(None),
            Poll::Pending => Poll::Pending,
        }
    }
}

impl InferenceStream {
    /// Close the stream.
    pub fn close(&mut self) {
        self.inner_stream.close();
    }
}

/// An event from the stream.
#[derive(Debug)]
pub enum StreamEvent {
    /// Emitted when the stream first opens.
    Open,

    /// A message containing data.
    Message(Data),

    /// Emitted when the stream is closed.
    Close,
}

impl From<reqwest_eventsource::Error> for StreamError {
    fn from(value: reqwest_eventsource::Error) -> Self {
        match value {
            reqwest_eventsource::Error::StreamEnded => StreamError::StreamClose,
            _ => StreamError::Unknown,
        }
    }
}

/// An error type for streaming.
#[derive(Debug)]
pub enum StreamError {
    /// An error emitted when the stream closes.
    StreamClose,

    /// All other error conditions.
    Unknown,
}

/// Represents a token.
#[derive(Debug, Serialize, Deserialize)]
pub struct Token {
    // id: u32,
    text: String,
    // logprob: f64,
    // special: bool,
}

#[allow(missing_docs)]
impl Token {
    // #[must_use]
    // pub fn id(&self) -> u32 {
    //     self.id
    // }

    pub fn new(text: String) -> Self {
        Self { text }
    }

    #[must_use]
    pub fn text(&self) -> &str {
        self.text.as_ref()
    }

    // #[must_use]
    // pub fn logprob(&self) -> f64 {
    //     self.logprob
    // }

    // #[must_use]
    // pub fn special(&self) -> bool {
    //     self.special
    // }
}

/// Part of a generated response.
#[derive(Debug, Serialize, Deserialize)]
#[allow(missing_docs)]
pub struct Data {
    pub token: Token,
    pub generated_text: Option<String>,
    pub details: Option<String>,
}

impl Data {
    /// Get the token.
    #[must_use]
    pub fn token(&self) -> &Token {
        &self.token
    }

    /// Get the generated text.
    #[must_use]
    pub fn generated_text(&self) -> Option<&String> {
        self.generated_text.as_ref()
    }

    /// Get the details.
    #[must_use]
    pub fn details(&self) -> Option<&String> {
        self.details.as_ref()
    }
}
