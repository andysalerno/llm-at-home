use graphs::Action;
use log::debug;

use crate::{
    model::{self, ChatCompletionRequest, ModelClient},
    state::ConversationState,
    tool::Tool,
};

pub fn agent_node(
    model_name: impl Into<String>,
    model: Box<dyn ModelClient>, // todo: expect some lazy model provider fn, not a model
    tools: Vec<Box<dyn Tool>>,
) -> Action<ConversationState> {
    let model_name = model_name.into();
    Action::new(
        "responding_agent",
        Box::new(move |state| {
            // Here you can implement the logic for your agent
            // For example, you can modify the state or perform some actions

            let messages = state.messages().to_vec();

            let tools = &tools;

            let response = model.get_model_response(&ChatCompletionRequest::new(
                &model_name,
                messages,
                None,
                None,
                Some(tools.iter().map(|t| t.as_ref().into()).collect()),
                None,
            ));

            debug!("Model response: {response:?}");

            let choices = response.choices;
            let response_message = &choices
                .first()
                .expect("expected at least one choice")
                .message;

            state.with_added_message(response_message.clone().into())
        }),
    )
}
