use crate::tool::ToolSchema;
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
    pub fn new(
        id: String,
        object: String,
        created: i64,
        model: String,
        choices: Vec<Choice>,
    ) -> Self {
        Self {
            id,
            object,
            created,
            model,
            choices,
        }
    }

    pub fn take_choices(self) -> Vec<Choice> {
        self.choices
    }
}

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct Message {
    role: String,
    content: String,
    tool_calls: Option<Vec<ToolCall>>,
    tool_call_id: Option<String>,
}

impl Message {
    pub fn new(
        role: impl Into<String>,
        content: impl Into<String>,
        tool_calls: Option<Vec<ToolCall>>,
    ) -> Self {
        Self {
            role: role.into(),
            content: content.into(),
            tool_calls,
            tool_call_id: None,
        }
    }

    pub fn with_tool_call_id(mut self, tool_call_id: Option<String>) -> Self {
        self.tool_call_id = tool_call_id;
        self
    }

    pub fn role(&self) -> &str {
        &self.role
    }

    pub fn content(&self) -> &str {
        &self.content
    }

    pub fn tool_calls(&self) -> Option<&Vec<ToolCall>> {
        self.tool_calls.as_ref()
    }

    pub fn tool_call_id(&self) -> Option<&String> {
        self.tool_call_id.as_ref()
    }
}

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct ToolCall {
    id: String,
    index: usize,
    r#type: String,
    function: FunctionCall,
}

impl ToolCall {
    pub fn new(id: String, index: usize, r#type: String, function: FunctionCall) -> Self {
        Self {
            id,
            index,
            r#type,
            function,
        }
    }

    pub fn id(&self) -> &str {
        &self.id
    }

    pub fn index(&self) -> usize {
        self.index
    }

    pub fn r#type(&self) -> &str {
        &self.r#type
    }

    pub fn function(&self) -> &FunctionCall {
        &self.function
    }
}

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct FunctionCall {
    /// A json object representing the arguments to the function
    arguments: String,

    /// The name of the function to call
    name: String,
}

impl FunctionCall {
    pub fn new(arguments: String, name: String) -> Self {
        Self { arguments, name }
    }

    pub fn name(&self) -> &str {
        &self.name
    }

    pub fn arguments(&self) -> &str {
        &self.arguments
    }
}

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct Choice {
    index: i32,
    message: Message,
    finish_reason: String,
}

impl Choice {
    pub fn new(index: i32, message: Message, finish_reason: String) -> Self {
        Self {
            index,
            message,
            finish_reason,
        }
    }

    pub fn message(&self) -> &Message {
        &self.message
    }
}

impl From<Message> for crate::state::Message {
    fn from(value: Message) -> Self {
        let tool_call_id = value.tool_call_id().cloned();
        Self::new(value.role, value.content)
            .with_tool_calls(
                value
                    .tool_calls
                    .map(|calls| calls.into_iter().map(std::convert::Into::into).collect()),
            )
            .with_tool_call_id(tool_call_id)
    }
}

impl From<crate::state::Message> for Message {
    fn from(value: crate::state::Message) -> Self {
        Self::new(
            value.role(),
            value.content(),
            value
                .tool_calls()
                .as_ref()
                .map(|calls| calls.iter().map(ToolCall::from).collect()),
        )
        .with_tool_call_id(value.tool_call_id().cloned())
    }
}

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct Tool {
    function: Function,
}

impl Tool {
    pub fn function(&self) -> &Function {
        &self.function
    }

    pub fn r#type(&self) -> &'static str {
        "function"
    }

    pub fn new(
        name: impl Into<String>,
        description: impl Into<String>,
        parameters: impl Into<ToolSchema>,
        strict: bool,
    ) -> Self {
        Self {
            function: Function {
                name: name.into(),
                description: description.into(),
                parameters: parameters.into(),
                strict,
            },
        }
    }
}

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct Function {
    name: String,
    description: String,
    parameters: ToolSchema,
    strict: bool,
}

impl Function {
    pub fn name(&self) -> &str {
        &self.name
    }

    pub fn description(&self) -> &str {
        &self.description
    }

    pub fn parameters(&self) -> &ToolSchema {
        &self.parameters
    }

    pub fn strict(&self) -> bool {
        self.strict
    }
}

impl From<&crate::state::ToolCall> for ToolCall {
    fn from(value: &crate::state::ToolCall) -> Self {
        Self {
            id: value.id().to_string(),
            index: value.index(),
            r#type: "function".to_string(),
            function: FunctionCall {
                arguments: value.function().arguments().to_string(),
                name: value.function().name().to_string(),
            },
        }
    }
}

impl From<ToolCall> for crate::state::ToolCall {
    fn from(value: ToolCall) -> Self {
        Self::new(value.id, value.index, value.r#type, value.function.into())
    }
}

impl From<FunctionCall> for crate::state::FunctionCall {
    fn from(value: FunctionCall) -> Self {
        Self::new(value.arguments, value.name)
    }
}
