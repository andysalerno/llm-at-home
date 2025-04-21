use graphs::Action;

use crate::state::{ConversationState, Message};

pub fn user_input_node() -> Action<ConversationState> {
    Action::new(
        "user_input",
        Box::new(move |state| {
            // get user input from command line:
            let mut input = String::new();
            println!("User:");
            std::io::stdin()
                .read_line(&mut input)
                .expect("Failed to read line");

            state.with_added_message(Message::new("user".to_string(), input.trim().to_string()))
        }),
    )
}
