use graphs::Action;

use crate::{
    model::{ChatCompletionRequest, ModelClient},
    state::ConversationState,
    tool::Tool,
};

pub fn tool_node(
    model: Box<dyn ModelClient>, // todo: expect some lazy model provider fn, not a model
    tools: &[Box<dyn Tool>],
) -> Action<ConversationState> {
}
