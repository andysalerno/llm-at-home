use graphs::{Action, Condition};

use crate::{
    model::{ChatCompletionRequest, ModelClient},
    state::ConversationState,
    tool::Tool,
};

pub fn response_has_tool_node() -> Condition<ConversationState> {
    Condition::new(Box::new(move |state| true))
}
