use graphs::Action;
use graphs_ai::{
    state::{ConversationState, Message},
    tool::{self, Tool},
};
use log::info;

pub fn invoke_tool(available_tools: Vec<Box<dyn Tool>>) -> Action<ConversationState> {
    Action::new(
        "invoke_tool",
        Box::new(move |state| {
            let last_message = state
                .messages()
                .last()
                .expect("expected at least one message");

            let tool_calls = last_message.tool_calls();

            debug_assert!(!tool_calls.is_empty());

            let first_tool_call = &tool_calls[0];

            let tool = available_tools
                .iter()
                .find(|tool| tool.name() == first_tool_call.function().name());

            let tool = tool.expect("Tool not found");

            info!("Invoking tool: {:?}", tool.name());

            let output = tool.get_output(first_tool_call.function().arguments());

            state.with_added_message(Message::new("user", output))
        }),
    )
}
