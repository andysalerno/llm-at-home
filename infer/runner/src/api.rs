use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize, Debug)]
pub(crate) struct Message {
    role: String,
    content: String,
}

impl Message {
    pub(crate) fn new(role: impl Into<String>, content: impl Into<String>) -> Self {
        Self {
            role: role.into(),
            content: content.into(),
        }
    }

    pub(crate) fn role(&self) -> &str {
        self.role.as_ref()
    }

    pub(crate) fn content(&self) -> &str {
        self.content.as_ref()
    }
}

#[derive(Serialize, Debug)]
pub(crate) struct ChatStreamEvent {
    id: String,
    object: String,
    created: usize,
    model: String,
    choices: Vec<ChatChoice>,
    system_fingerprint: String,
}

impl ChatStreamEvent {
    pub(crate) fn new(model: String, choices: Vec<ChatChoice>) -> Self {
        Self {
            id: String::new(),
            object: "chat.completion.chunk".into(),
            created: 0,
            model,
            choices,
            system_fingerprint: String::new(),
        }
    }
}

#[derive(Serialize, Debug)]
pub(crate) struct ChatChoice {
    index: usize,
    delta: Message,
    finish_reason: Option<String>,
}

impl ChatChoice {
    pub(crate) fn new(delta: Message, finish_reason: Option<String>) -> Self {
        Self {
            index: 0,
            delta,
            finish_reason,
        }
    }
}

#[allow(unused)]
#[derive(Deserialize, Debug)]
pub(crate) struct Request {
    messages: Vec<Message>,
    model: String,
    stream: bool,
    max_tokens: usize,
    stop: Vec<String>,
    temperature: f32,
    top_p: f32,
    frequency_penalty: f32,
}

#[allow(unused)]
impl Request {
    pub(crate) fn messages(&self) -> &[Message] {
        self.messages.as_ref()
    }

    pub(crate) fn model(&self) -> &str {
        self.model.as_ref()
    }

    pub(crate) fn stream(&self) -> bool {
        self.stream
    }

    pub(crate) fn max_tokens(&self) -> usize {
        self.max_tokens
    }

    pub(crate) fn stop(&self) -> &[String] {
        self.stop.as_ref()
    }

    pub(crate) fn temperature(&self) -> f32 {
        self.temperature
    }

    pub(crate) fn top_p(&self) -> f32 {
        self.top_p
    }

    pub(crate) fn frequency_penalty(&self) -> f32 {
        self.frequency_penalty
    }
}
