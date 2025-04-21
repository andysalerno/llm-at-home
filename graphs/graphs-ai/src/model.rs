use serde::{Deserialize, Serialize};

pub trait ModelClient {
    fn get_model_response(&self, request: &ChatCompletionRequest) -> ChatCompletionResponse;
}

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct ChatCompletionRequest {
    messages: Vec<Message>,
    temperature: Option<f32>,
    // top_p: f32,
    stream: bool,
    // stop: Option<String>,
    max_completion_tokens: Option<usize>,
    tools: Option<Vec<Tool>>,

    // TODO: create an enum to represent the options, none/auto/required
    tool_choice: Option<String>,
    // presence_penalty: Option<f32>,
    // frequency_penalty: Option<f32>,
}

impl ChatCompletionRequest {
    pub fn new(
        messages: Vec<Message>,
        temperature: Option<f32>,
        max_completion_tokens: Option<usize>,
        tools: Option<Vec<Tool>>,
        tool_choice: Option<String>,
    ) -> Self {
        Self {
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

    pub fn max_completion_tokens(&self) -> Option<usize> {
        self.max_completion_tokens
    }

    pub fn tools(&self) -> Option<&Vec<Tool>> {
        self.tools.as_ref()
    }

    pub fn tool_choice(&self) -> Option<&String> {
        self.tool_choice.as_ref()
    }
}

#[derive(Serialize, Deserialize, Debug, Clone)]
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

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct Tool {
    function: Function,
    r#type: String,
}

impl Tool {
    pub fn function(&self) -> &Function {
        &self.function
    }

    pub fn r#type(&self) -> &str {
        &self.r#type
    }
}

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct Function {
    name: String,
    description: String,
    parameters: String,
    strict: bool,
}

impl Function {
    pub fn name(&self) -> &str {
        &self.name
    }

    pub fn description(&self) -> &str {
        &self.description
    }

    pub fn parameters(&self) -> &str {
        &self.parameters
    }

    pub fn strict(&self) -> bool {
        self.strict
    }
}
