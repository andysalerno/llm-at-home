use serde::{Deserialize, Serialize};

#[derive(Clone, Debug)]
pub struct ConversationState {
    messages: Vec<Message>,
}

impl ConversationState {
    #[must_use] pub const fn new(messages: Vec<Message>) -> Self {
        Self { messages }
    }

    #[must_use] pub const fn empty() -> Self {
        Self {
            messages: Vec::new(),
        }
    }

    #[must_use] pub fn messages(&self) -> &[Message] {
        &self.messages
    }

    pub fn add_message(&mut self, message: Message) {
        self.messages.push(message);
    }
}

#[derive(Clone, Serialize, Deserialize, Debug)]
pub struct AgentName(String);

impl AgentName {
    pub fn new(name: impl Into<String>) -> Self {
        Self(name.into())
    }
}

#[derive(Clone, Serialize, Deserialize, Debug)]
pub struct Role(String);

impl Role {
    pub fn new(role: impl Into<String>) -> Self {
        Self(role.into())
    }
}

#[derive(Clone, Serialize, Deserialize, Debug)]
pub struct Message {
    agent_name: AgentName,
    role: Role,
    content: String,
}

impl Message {
    #[must_use] pub const fn new(agent_name: AgentName, role: Role, content: String) -> Self {
        Self {
            agent_name,
            role,
            content,
        }
    }
}
