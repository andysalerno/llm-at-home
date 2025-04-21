use graphs::Action;

use crate::{
    model::{ChatCompletionRequest, Message, ModelClient},
    state::ConversationState,
    tool::Tool,
};

pub fn agent_node(
    model: Box<dyn ModelClient>, // todo: expect some lazy model provider fn, not a model
    tools: &[Box<dyn Tool>],
) -> Action<ConversationState> {
    Action::new(Box::new(move |state| {
        // Here you can implement the logic for your agent
        // For example, you can modify the state or perform some actions

        let messages = vec![
            Message::new("system", "You are a helpful assistant."),
            Message::new("user", "hi"),
        ];

        let response = model.get_model_response(&ChatCompletionRequest::new(
            "model",
            messages,
            0.7,
            Some(100),
        ));

        let choices = response.take_choices();
        let response_message = choices
            .first()
            .expect("expected at least one choice")
            .message();

        state.with_added_message(response_message.clone().into())
    }))
}
