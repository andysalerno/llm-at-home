use graphs::Action;

use crate::{
    model::{ChatCompletionRequest, ModelClient},
    state::ConversationState,
    tool::Tool,
};

pub fn agent_node(
    model: Box<dyn ModelClient>, // todo: expect some lazy model provider fn, not a model
    tools: Vec<Box<dyn Tool>>,
) -> Action<ConversationState> {
    Action::new(
        "responding_agent",
        Box::new(move |state| {
            // Here you can implement the logic for your agent
            // For example, you can modify the state or perform some actions

            let messages = state
                .messages()
                .iter()
                .cloned()
                .map(std::convert::Into::into)
                .collect::<Vec<_>>();

            let tools = &tools;

            let response = model.get_model_response(&ChatCompletionRequest::new(
                messages, None, None, None, None,
            ));

            let choices = response.take_choices();
            let response_message = choices
                .first()
                .expect("expected at least one choice")
                .message();

            state.with_added_message(response_message.clone().into())
        }),
    )
}
