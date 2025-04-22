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
    tool_calls: Vec<ToolCall>,
}

impl Message {
    pub fn new(role: impl Into<String>, content: impl Into<String>) -> Self {
        Self {
            role: role.into(),
            content: content.into(),
            tool_calls: Vec::new(),
        }
    }

    pub fn role(&self) -> &str {
        &self.role
    }

    pub fn content(&self) -> &str {
        &self.content
    }

    pub fn tool_calls(&self) -> &[ToolCall] {
        &self.tool_calls
    }
}

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct ToolCall {
    id: String,
    function: Function,
}

impl ToolCall {
    pub fn id(&self) -> &str {
        &self.id
    }

    pub fn function(&self) -> &Function {
        &self.function
    }
}

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct Function {
    arguments: String,
    name: String,
}

impl Function {
    pub fn arguments(&self) -> &str {
        &self.arguments
    }

    pub fn name(&self) -> &str {
        &self.name
    }
}
