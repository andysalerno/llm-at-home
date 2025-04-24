use graphs::Action;

use crate::state::{ConversationState, Message};

pub fn remove_system_prompt() -> Action<ConversationState> {
    Action::new(
        "remove_system_prompt",
        Box::new(move |state| state.without_messages_having_role("system")),
    )
}

#[derive(Clone, Copy, Debug)]
pub enum SystemPromptLocation {
    FirstMessage,
    LastMessage,
}

pub fn add_system_prompt(
    content: impl Into<String>,
    location: SystemPromptLocation,
) -> Action<ConversationState> {
    let content = content.into();

    Action::new(
        "add_system_prompt",
        Box::new(move |state| match location {
            SystemPromptLocation::FirstMessage => {
                state.with_added_message_to_front(Message::new("system", content.clone()))
            }
            SystemPromptLocation::LastMessage => {
                state.with_added_message(Message::new("system", content.clone()))
            }
        }),
    )
}
