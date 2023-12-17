use chat::{history::History, Message, Renderer, Role};

use chat_formats::detect_chat_template;
use libinfer::{chat_client::ChatClient, function::Function, llm_client::LLMClient, read_prompt};
use log::{debug, info};
use regex::Regex;

pub(crate) async fn chat_loop(
    client: impl LLMClient + Send + Sync + 'static,
    functions: Vec<Box<dyn Function + Send + Sync + 'static>>,
) {
    let info = client.get_info().await;
    let chat_template = detect_chat_template(info.model_id());

    let chat_client = ChatClient::new(Box::new(client), chat_template);

    let mut history = {
        let mut history = History::new();

        let prompt = read_prompt("system.txt");
        history.add(Message::new(Role::System, prompt));

        history
    };

    loop {
        let user_input = get_user_input();
        let next_message = {
            debug!("Saw user input: {user_input}");
            Message::new(Role::User, user_input.clone())
        };

        // add the user's message
        history.add(next_message);

        let function_call = select_function(&chat_client, &history).await;

        info!("LLM provided function call: {function_call:?}");

        let function = map_to_function(&function_call, functions.as_slice()).unwrap_or_else(|| {
            panic!(
                "Function invoked, but does not exist: {}",
                function_call.name()
            )
        });

        let function_output = function
            .get_output(function_call.args(), &chat_client)
            .await;

        info!("got function output:\n{function_output}");

        if !function_output.is_empty() {
            debug!("Skipping empty function output");
            history.add(Message::new(Role::Function, function_output));
        }

        let assistant_response = chat_client.get_assistant_response(&history).await;

        info!("Got assistant response: '{}'", assistant_response.content());

        history.add(assistant_response);

        let new_history_render = Renderer::render(&history, chat_client.chat_template());
        info!("new history:\n{new_history_render}");
    }
}

pub(crate) fn map_to_function<'a>(
    function_call: &'a FunctionCall,
    functions: &'a [Box<dyn Function + Send + Sync + 'static>],
) -> Option<&'a (dyn Function + Send + Sync + 'static)> {
    functions
        .iter()
        .find(|f| f.name() == function_call.name())
        .map(std::convert::AsRef::as_ref)
}

#[derive(Debug, Clone)]
pub(crate) struct FunctionCall {
    name: String,
    args: String,
}

impl FunctionCall {
    fn parse(input: &str) -> FunctionCall {
        let re = Regex::new(r#"(\w+)\(['"]?([^'"]*)['"]?\)"#).unwrap();

        let captures = re
            .captures(input)
            .unwrap_or_else(|| panic!("Expected to parse as a function, but saw: '{input}'"));

        FunctionCall {
            name: captures[1].to_owned(),
            args: captures[2].to_owned(),
        }
    }

    pub fn name(&self) -> &str {
        self.name.as_ref()
    }

    pub fn args(&self) -> &str {
        self.args.as_ref()
    }
}

pub(crate) async fn select_function(client: &ChatClient, history: &History) -> FunctionCall {
    let system_template = read_prompt("action_selection_new_user.txt");
    let assistant_prompt_template = read_prompt("action_selection_new_assistant.txt");

    let chat_template = client.chat_template();

    let alt_history = {
        let mut alt_history = history.clone();
        alt_history.set_initial_system_message(system_template);
        alt_history.add(Message::new(Role::Assistant, assistant_prompt_template));
        alt_history
    };

    let rendered_prompt = Renderer::render(&alt_history, chat_template);

    info!("rendered prompt:\n{rendered_prompt}");

    let results = client.get_response_for_template(&rendered_prompt).await;

    info!("got function results: {results:#?}");

    let invocation = results
        .get("function_call")
        .expect("expected invocation to be present");
    info!("invocation: {invocation}");

    FunctionCall::parse(invocation)
}
