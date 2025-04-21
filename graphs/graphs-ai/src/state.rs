use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize, Debug)]
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
}

impl Default for ConversationState {
    fn default() -> Self {
        Self::new()
    }
}

#[derive(Serialize, Deserialize, Debug)]
pub struct Message {
    role: String,
    content: String,
}

impl Message {
    pub fn new(role: String, content: String) -> Self {
        Self { role, content }
    }
}
