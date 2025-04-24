use graphs::Action;
use graphs_ai::{model::Message, state::ConversationState, tool::Tool};
use log::info;

pub fn invoke_tool(available_tools: Vec<Box<dyn Tool>>) -> Action<ConversationState> {
    Action::new(
        "invoke_tool",
        Box::new(move |state| {
            let last_message = state
                .messages()
                .last()
                .expect("expected at least one message");

            let tool_calls = &last_message.tool_calls;

            debug_assert!(tool_calls.as_ref().is_some_and(|calls| !calls.is_empty()));

            let first_tool_call = tool_calls.as_ref().unwrap().first().unwrap();
            let tool_call_id = first_tool_call.id.clone();

            let tool = available_tools
                .iter()
                .find(|tool| tool.name() == first_tool_call.function.name);

            let tool = tool.expect("Tool not found");

            info!("Invoking tool: {:?}", tool.name());

            let output = tool.get_output(&first_tool_call.function.arguments);

            let message = {
                let mut message = Message::new("tool", output);
                message.tool_call_id = Some(tool_call_id);

                message
            };

            state.with_added_message(message)
        }),
    )
}
