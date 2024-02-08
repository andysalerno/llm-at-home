use crate::{
    document::{Document, DocumentChunk, Gen},
    embeddings::{EmbeddingsRequest, EmbeddingsResponse},
    generate_request::{GenerateParametersBuilder, GenerateRequest},
    llm_client::{InferenceStream, LLMClient, StreamError, StreamEvent},
};
use chat::{
    history::{History, RenderHistorySettingsBuilder},
    ChatTemplate, Message, Renderer, Role,
};
use futures_util::StreamExt;
use log::{debug, info, warn};
use std::collections::HashMap;

/// A chat client, which takes in a `LLMClient` and extends it with
/// chat functionality
pub struct ChatClient {
    llm_client: Box<dyn LLMClient + Send + Sync>,
    chat_template: ChatTemplate,
}

impl ChatClient {
    /// Create a new `ChatClient` with the given llm client and template.
    #[must_use]
    pub fn new(llm_client: Box<dyn LLMClient + Send + Sync>, chat_template: ChatTemplate) -> Self {
        Self {
            llm_client,
            chat_template,
        }
    }

    /// Generate the next assistant message based on the given history.
    #[must_use]
    pub fn get_assistant_response_stream(&self, history: &History) -> InferenceStream {
        let eos = self.chat_template.eos_token().to_owned();

        let rendered_history = Renderer::render_with_settings(
            history,
            &self.chat_template,
            &RenderHistorySettingsBuilder::default()
                .nudge_assistant(true)
                .build()
                .unwrap(),
        );

        let parameters = GenerateParametersBuilder::default()
            .max_new_tokens(Some(800))
            .do_sample(Some(true))
            .temperature(Some(0.2))
            .stop(Some(vec![eos.clone()]))
            .build()
            .unwrap();

        let request = GenerateRequest::new(rendered_history, parameters);

        {
            let json = serde_json::to_string_pretty(&request).unwrap();
            debug!("Request is:\n{json}");
        }

        self.llm_client.get_response_stream(&request)
    }

    /// Generate the next assistant message based on the given history.
    pub async fn get_assistant_response(&self, history: &History) -> Message {
        let eos = self.chat_template.eos_token().to_owned();

        let mut stream = self.get_assistant_response_stream(history);

        let mut assistant_response = String::new();

        while let Some(event) = stream.next().await {
            match event {
                Ok(StreamEvent::Open) => debug!("connection open"),
                Ok(StreamEvent::Message(m)) => {
                    assistant_response.push_str(m.token().text());
                }
                Err(StreamError::StreamClose) => {
                    debug!("stream complete");
                    stream.close();
                }
                _ => {
                    debug!("unknown event");
                    stream.close();
                }
            }
        }

        assistant_response = assistant_response.trim_end().to_owned();

        if assistant_response.ends_with(&eos) {
            assistant_response = assistant_response.strip_suffix(&eos).unwrap().into();
        }

        Message::new(Role::Assistant, assistant_response)
    }

    /// Generate a response given the template.
    pub async fn get_response_for_template(&self, template: &str) -> HashMap<String, String> {
        let document = Document::parse(template);

        let mut results = HashMap::new();

        // Loop over chunks.
        // When you encounter static text, just add it to the working document.
        // When you encounter a 'gen', trigger the LLM to fill in text until the stop tokens.
        let mut processing_text = String::new();
        for chunk in document.chunks() {
            match chunk {
                DocumentChunk::Text(text) => processing_text.push_str(text),
                DocumentChunk::Gen(Gen { name, properties }) => {
                    // ask llm to gen until you hit the stop tokens

                    let stops = properties
                        .get("stop")
                        .expect("Expected a stop value, but found none");
                    let stops = vec![stops.to_owned()];

                    let parameters = GenerateParametersBuilder::default()
                        .max_new_tokens(Some(
                            properties
                                .get("max_new_tokens")
                                .map_or("800", std::string::String::as_str)
                                .parse()
                                .unwrap(),
                        ))
                        .temperature(properties.get("temperature").map(|t| t.parse().unwrap()))
                        .top_k(properties.get("top_k").map(|t| t.parse().unwrap()))
                        .top_p(properties.get("top_p").map(|t| t.parse().unwrap()))
                        .stop(Some(stops.clone()))
                        .build()
                        .unwrap();

                    let request = GenerateRequest::new(processing_text.clone(), parameters);
                    {
                        let json = serde_json::to_string_pretty(&request).unwrap();
                        debug!("Request is:\n{json}");
                    }

                    let response = self.llm_client.get_response(&request).await;
                    debug!("response (pre-trim) is: {response}");

                    let response = if let Some((before, after)) =
                        response.split_once(stops.first().unwrap())
                    {
                        if !after.is_empty() {
                            let pretty = after.replace('\n', "\\n");
                            warn!("stop content removed from result: '{pretty}'");
                        }

                        before.to_owned()
                    } else {
                        response
                    };

                    processing_text.push_str(&response);

                    results.insert(name.clone(), response.clone());

                    info!("response is: {response}");
                }
            }
        }

        debug!("Document is:\n{document:#?}");

        results
    }

    /// Gets the chat template.
    #[must_use]
    pub fn chat_template(&self) -> &ChatTemplate {
        &self.chat_template
    }

    /// Generate embeddings for the given input.
    pub async fn get_embeddings(&self, request: &EmbeddingsRequest) -> EmbeddingsResponse {
        self.llm_client.get_embeddings(request).await
    }
}
