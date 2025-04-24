use serde::{Deserialize, Serialize};

use crate::model::Message;

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
        new_state.messages.retain(|message| message.role() != role);
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
