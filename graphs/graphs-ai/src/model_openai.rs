use std::{borrow::Borrow, convert};

use crate::model::ModelClient;
use log::{debug, info};
use serde::{Deserialize, Serialize};
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
        let request: OpenAIChatCompletionRequest = convert_request(request, &self.model);

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

        serde_json::from_value::<crate::model::ChatCompletionResponse>(response_json)
            .expect("Failed to parse OpenAI response")
    }
}

#[derive(Serialize, Deserialize, Debug)]
struct OpenAIChatCompletionRequest {
    model: String,
    messages: Vec<Message>,
    #[serde(skip_serializing_if = "Option::is_none")]
    temperature: Option<f32>,
    // top_p: f32,
    stream: bool,
    // stop: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    max_completion_tokens: Option<usize>,
    #[serde(skip_serializing_if = "Option::is_none")]
    tools: Option<Vec<Tool>>,

    // TODO: create an enum to represent the options, none/auto/required
    #[serde(skip_serializing_if = "Option::is_none")]
    tool_choice: Option<String>,
    // presence_penalty: Option<f32>,
    // frequency_penalty: Option<f32>,
}

impl OpenAIChatCompletionRequest {
    pub fn new(
        model: impl Into<String>,
        messages: Vec<Message>,
        temperature: Option<f32>,
        max_completion_tokens: Option<usize>,
        tools: Option<Vec<Tool>>,
        tool_choice: Option<String>,
    ) -> Self {
        Self {
            model: model.into(),
            messages,
            temperature,
            stream: false,
            max_completion_tokens,
            tools,
            tool_choice,
        }
    }

    pub fn messages(&self) -> &[Message] {
        &self.messages
    }

    pub fn temperature(&self) -> Option<f32> {
        self.temperature
    }
}

#[derive(Serialize, Deserialize, Debug)]
struct ChatCompletionResponse {
    id: String,
    object: String,
    created: i64,
    model: String,
    choices: Vec<Choice>,
}

impl ChatCompletionResponse {
    pub fn take_choices(self) -> Vec<Choice> {
        self.choices
    }
}

#[derive(Serialize, Deserialize, Debug, Clone)]
struct Message {
    role: String,
    content: String,
}

impl Message {
    pub fn new(role: impl Into<String>, content: impl Into<String>) -> Self {
        Self {
            role: role.into(),
            content: content.into(),
        }
    }

    pub fn role(&self) -> &str {
        &self.role
    }

    pub fn content(&self) -> &str {
        &self.content
    }
}

#[derive(Serialize, Deserialize, Debug, Clone)]
struct Choice {
    index: i32,
    message: Message,
    finish_reason: String,
}

impl Choice {
    pub fn message(&self) -> &Message {
        &self.message
    }
}

impl From<Message> for crate::state::Message {
    fn from(value: Message) -> Self {
        Self::new(value.role, value.content)
    }
}

impl From<crate::state::Message> for Message {
    fn from(value: crate::state::Message) -> Self {
        Self::new(value.role(), value.content())
    }
}

impl<T> From<T> for Message
where
    T: Borrow<crate::model::Message>,
{
    fn from(value: T) -> Self {
        let value = value.borrow();
        Self {
            role: value.role().to_string(),
            content: value.content().to_string(),
        }
    }
}

#[derive(Serialize, Deserialize, Debug, Clone)]
struct Tool {
    function: Function,
    r#type: String,
}

#[derive(Serialize, Deserialize, Debug, Clone)]
struct Function {
    name: String,
    description: String,
    parameters: String,
    strict: bool,
}

fn convert_request(
    other: &crate::model::ChatCompletionRequest,
    model: impl Into<String>,
) -> OpenAIChatCompletionRequest {
    OpenAIChatCompletionRequest::new(
        model.into(),
        other.messages().iter().map(Message::from).collect(),
        other.temperature(),
        other.max_completion_tokens(),
        other
            .tools()
            .map(|tools| tools.iter().map(Tool::from).collect()),
        other.tool_choice().cloned(),
    )
}

impl<T> From<T> for Tool
where
    T: Borrow<crate::model::Tool>,
{
    fn from(value: T) -> Self {
        let value = value.borrow();
        Self {
            function: value.function().into(),
            r#type: value.r#type().to_string(),
        }
    }
}

impl From<&crate::model::Function> for Function {
    fn from(value: &crate::model::Function) -> Self {
        Self {
            name: value.name().to_string(),
            description: value.description().to_string(),
            parameters: value.parameters().to_string(),
            strict: value.strict(),
        }
    }
}
