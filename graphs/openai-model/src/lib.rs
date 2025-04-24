use graphs_ai::{model::ModelClient, tool::ToolSchema};
use log::{debug, info};
use serde::{Deserialize, Serialize};
use serde_json::Value;
use std::borrow::Borrow;

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

        let openai_response = serde_json::from_value::<OpenAIChatCompletionResponse>(response_json)
            .expect("Failed to parse OpenAI response");

        debug!("parsed openai response: {openai_response:?}");

        openai_response.into()
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
struct OpenAIChatCompletionResponse {
    id: String,
    object: String,
    created: i64,
    model: String,
    choices: Vec<Choice>,
}

impl OpenAIChatCompletionResponse {
    fn new(id: String, object: String, created: i64, model: String, choices: Vec<Choice>) -> Self {
        Self {
            id,
            object,
            created,
            model,
            choices,
        }
    }
}

/// Convert from a standard model request to an `OpenAI` API request representation.
fn convert_request(
    other: &graphs_ai::model::ChatCompletionRequest,
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

/// Convert an `OpenAI` API response to a standard model response.
impl From<OpenAIChatCompletionResponse> for graphs_ai::model::ChatCompletionResponse {
    fn from(value: OpenAIChatCompletionResponse) -> Self {
        let model = value.model;
        let object = value.object;
        let created = value.created;
        let choices = value
            .choices
            .into_iter()
            .map(std::convert::Into::into)
            .collect();

        Self::new(value.id, object, created, model, choices)
    }
}

/////
///// Message
/////

/// A message sent or received from the `OpenAI` API.
#[derive(Serialize, Deserialize, Debug, Clone)]
struct Message {
    role: String,
    content: String,

    #[serde(skip_serializing_if = "Option::is_none")]
    tool_calls: Option<Vec<ToolCall>>,
}

impl Message {
    pub fn new(role: impl Into<String>, content: impl Into<String>) -> Self {
        Self {
            role: role.into(),
            content: content.into(),
            tool_calls: None,
        }
    }

    pub fn role(&self) -> &str {
        &self.role
    }

    pub fn content(&self) -> &str {
        &self.content
    }
}

/// Convert an `OpenAI` message to a standard model message
impl From<Message> for graphs_ai::model::Message {
    fn from(value: Message) -> Self {
        Self::new(
            value.role,
            value.content,
            value
                .tool_calls
                .map(|tools| tools.into_iter().map(std::convert::Into::into).collect()),
        )
    }
}

/// Convert an `OpenAI` message to a conversation state model message
impl From<Message> for graphs_ai::state::Message {
    fn from(value: Message) -> Self {
        Self::new(value.role, value.content).with_tool_calls(value.tool_calls.map(|tools| {
            tools
                .into_iter()
                .map(std::convert::Into::into)
                .collect::<Vec<_>>()
        }))
    }
}

/// Convert a standard model message to an `OpenAI` message
impl From<graphs_ai::model::Message> for Message {
    fn from(value: graphs_ai::model::Message) -> Self {
        // TODO: move, don't clone
        Self {
            role: value.role().to_string(),
            content: value.content().to_string(),
            tool_calls: value
                .tool_calls()
                .as_ref()
                .map(|tools| tools.iter().map(std::convert::Into::into).collect()),
        }
    }
}

impl From<&graphs_ai::model::Message> for Message {
    fn from(value: &graphs_ai::model::Message) -> Self {
        Self {
            role: value.role().to_string(),
            content: value.content().to_string(),
            tool_calls: value
                .tool_calls()
                .as_ref()
                .map(|tools| tools.iter().map(std::convert::Into::into).collect()),
        }
    }
}

/// Convert a conversation state model message to an `OpenAI` message
impl From<graphs_ai::state::Message> for Message {
    fn from(value: graphs_ai::state::Message) -> Self {
        Self::new(value.role(), value.content())
    }
}

/////
///// Choice
/////

/// A choice received from the `OpenAI` API.
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
/// Convert an `OpenAI` choice to a standard model choice
impl From<Choice> for graphs_ai::model::Choice {
    fn from(value: Choice) -> Self {
        Self::new(value.index, value.message.into(), value.finish_reason)
    }
}

/////
///// Function
/////

/// A function description that can be sent to the `OpenAI` API.
#[derive(Serialize, Deserialize, Debug, Clone)]
struct Function {
    name: String,
    description: String,
    parameters: ToolSchema,
    strict: bool,
}

/// Convert a standard model function to an `OpenAI` function.
impl From<&graphs_ai::model::Function> for Function {
    fn from(value: &graphs_ai::model::Function) -> Self {
        Self {
            name: value.name().to_string(),
            description: value.description().to_string(),
            parameters: value.parameters().clone(),
            strict: value.strict(),
        }
    }
}

/////
///// FunctionCall
/////

/// A function invocation received from the `OpenAI` API.
#[derive(Serialize, Deserialize, Debug, Clone)]
struct FunctionCall {
    /// A json object representing the arguments to the function
    arguments: String,

    /// The name of the function to call
    name: String,
}
impl From<FunctionCall> for graphs_ai::model::FunctionCall {
    fn from(value: FunctionCall) -> Self {
        Self::new(value.arguments, value.name)
    }
}

impl From<graphs_ai::model::FunctionCall> for FunctionCall {
    fn from(value: graphs_ai::model::FunctionCall) -> Self {
        Self {
            arguments: value.arguments().into(),
            name: value.name().into(),
        }
    }
}

impl From<FunctionCall> for graphs_ai::state::FunctionCall {
    fn from(value: FunctionCall) -> Self {
        Self::new(value.arguments, value.name)
    }
}

/////
///// ToolCall
/////

/// A tool invocation received from the `OpenAI` API.
#[derive(Serialize, Deserialize, Debug, Clone)]
struct ToolCall {
    id: String,
    index: usize,
    r#type: String,
    function: FunctionCall,
}

/// From an `OpenAI` `ToolCall` to a standard model `ToolCall`.
impl From<ToolCall> for graphs_ai::model::ToolCall {
    fn from(value: ToolCall) -> Self {
        Self::new(value.id, value.index, value.r#type, value.function.into())
    }
}

impl From<&graphs_ai::model::ToolCall> for ToolCall {
    fn from(value: &graphs_ai::model::ToolCall) -> Self {
        Self {
            id: value.id().into(),
            index: value.index(),
            r#type: value.r#type().into(),
            function: value.function().clone().into(),
        }
    }
}

impl From<ToolCall> for graphs_ai::state::ToolCall {
    fn from(value: ToolCall) -> Self {
        Self::new(value.id, value.index, value.r#type, value.function.into())
    }
}

/////
///// Tool
/////

/// A tool description that can be sent to the `OpenAI` API.
#[derive(Serialize, Deserialize, Debug, Clone)]
struct Tool {
    function: Function,
    r#type: String,
}

impl<T> From<T> for Tool
where
    T: Borrow<graphs_ai::model::Tool>,
{
    fn from(value: T) -> Self {
        let value = value.borrow();
        Self {
            function: value.function().into(),
            r#type: value.r#type().to_string(),
        }
    }
}
