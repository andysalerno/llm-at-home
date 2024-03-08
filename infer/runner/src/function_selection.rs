use chat::{history::History, Message, Renderer, Role};
use functions::NoOp;
use libinfer::{chat_client::ChatClient, function::Function, read_prompt};
use log::info;
use regex::Regex;

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
        let input = input.replace("'", "");
        let re = Regex::new(r#"(\w+)\(['"]?([^'"]*)['"]?\)"#).unwrap();

        let captures = re
            .captures(&input)
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

pub(crate) async fn select_function(
    client: &ChatClient,
    functions: &[Box<dyn Function + Send + Sync>],
    history: &History,
) -> FunctionCall {
    let system_template = {
        let template = read_prompt("action_selection_new_user.txt");

        let function_descriptions_for_model = functions
            .iter()
            .filter(|f| f.name() != NoOp.name())
            .map(|f| f.description_for_model())
            .collect::<Vec<_>>()
            .join("\n\n");

        template.replace("{{functions}}", &function_descriptions_for_model)
    };

    let assistant_prompt_template = {
        let last_user_message = history
            .messages()
            .iter()
            .filter(|m| m.role() == &Role::User)
            .last()
            .expect("Should be at least one user message")
            .content()
            .replace('\"', "\\\"");

        let functions_names = functions
            .iter()
            .map(|f| format!("'{}'", f.name()))
            .collect::<Vec<_>>()
            .join(", ");

        read_prompt("action_selection_new_assistant.txt")
            .replace("{last_user_message}", &last_user_message)
            .replace("{function_names_list}", &functions_names)
    };

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
