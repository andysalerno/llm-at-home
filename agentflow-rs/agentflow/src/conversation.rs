use serde::{Deserialize, Serialize};

#[derive(Clone)]
pub struct ConversationState {
    messages: Vec<Message>,
}

impl ConversationState {
    pub const fn new(messages: Vec<Message>) -> Self {
        Self { messages }
    }

    pub const fn empty() -> Self {
        Self {
            messages: Vec::new(),
        }
    }
}

#[derive(Clone, Serialize, Deserialize)]
pub struct AgentName(String);

#[derive(Clone, Serialize, Deserialize)]
pub struct Role(String);

#[derive(Clone, Serialize, Deserialize)]
pub struct Message {
    agent_name: AgentName,
    role: Role,
    content: String,
}
