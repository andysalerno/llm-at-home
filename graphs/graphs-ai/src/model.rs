use serde::{Deserialize, Serialize};

pub trait ModelClient {
    fn get_model_response(&self, request: &ChatCompletionRequest) -> ChatCompletionResponse;
}

#[derive(Serialize, Deserialize, Debug)]
pub struct ChatCompletionRequest {
    model: String,
    messages: Vec<Message>,
    temperature: f32,
    // top_p: f32,
    stream: bool,
    // stop: Option<String>,
    max_completion_tokens: Option<usize>,
    // presence_penalty: Option<f32>,
    // frequency_penalty: Option<f32>,
}

impl ChatCompletionRequest {
    pub fn new(
        model: impl Into<String>,
        messages: Vec<Message>,
        temperature: f32,
        max_completion_tokens: Option<usize>,
    ) -> Self {
        Self {
            model: model.into(),
            messages,
            temperature,
            stream: false,
            max_completion_tokens,
        }
    }

    pub fn messages(&self) -> &[Message] {
        &self.messages
    }

    pub fn temperature(&self) -> f32 {
        self.temperature
    }
}

#[derive(Serialize, Deserialize, Debug)]
pub struct ChatCompletionResponse {
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
pub struct Message {
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
pub struct Choice {
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
