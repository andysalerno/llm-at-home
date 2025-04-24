use graphs::Condition;

use crate::state::ConversationState;

pub fn response_has_tool_node() -> Condition<ConversationState> {
    Condition::new(
        "has_tool",
        Box::new(move |state| {
            let last_message = state
                .messages()
                .last()
                .expect("expected at least one message");

            let tool_calls = last_message.tool_calls();

            match tool_calls {
                Some(calls) => !calls.is_empty(),
                None => false,
            }
        }),
    )
}
