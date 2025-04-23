use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct ConversationState {
    messages: Vec<Message>,
}

impl ConversationState {
    pub fn new() -> Self {
        Self {
            messages: Vec::new(),
        }
    }

    pub fn with_added_message(self, message: Message) -> Self {
        let mut new_state = self;
        new_state.messages.push(message);
        new_state
    }

    pub fn with_added_message_to_front(self, message: Message) -> Self {
        let mut new_state = self;
        new_state.messages.insert(0, message);
        new_state
    }

    pub fn without_messages_having_role(self, role: impl Into<String>) -> Self {
        let mut new_state = self;
        let role = role.into();
        new_state.messages.retain(|message| message.role != role);
        new_state
    }

    pub fn messages(&self) -> &[Message] {
        &self.messages
    }
}

impl Default for ConversationState {
    fn default() -> Self {
        Self::new()
    }
}

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct Message {
    role: String,
    content: String,
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

    pub fn with_tool_calls(mut self, tool_calls: Option<Vec<ToolCall>>) -> Self {
        self.tool_calls = tool_calls;
        self
    }

    pub fn role(&self) -> &str {
        &self.role
    }

    pub fn content(&self) -> &str {
        &self.content
    }

    pub fn tool_calls(&self) -> &Option<Vec<ToolCall>> {
        &self.tool_calls
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

    pub fn function(&self) -> &FunctionCall {
        &self.function
    }

    pub fn index(&self) -> usize {
        self.index
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

    pub fn arguments(&self) -> &str {
        &self.arguments
    }

    pub fn name(&self) -> &str {
        &self.name
    }
}
