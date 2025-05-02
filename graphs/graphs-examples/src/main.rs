mod invoke_tool;
mod weather_tool;

use graphs::GraphRunner;
use graphs_ai::{
    agent::agent_node,
    response_has_tools_node::response_has_tool_node,
    state::ConversationState,
    system_prompt_node::{SystemPromptLocation, add_system_prompt, remove_system_prompt},
    tool::{Tool, ToolDescription},
    user::user_input_node,
};
use graphs_mcp::McpContext;
use invoke_tool::invoke_tool;
use log::info;
use openai_model::OpenAIModel;
use weather_tool::WeatherTool;

fn main() {
    env_logger::init();

    let mcp_tools = {
        let mcp_client = McpContext::connect("http://localhost:8002/sse").unwrap();
        let tools = mcp_client.get_tools().unwrap();
        // let model_tools = tools
        //     .iter()
        //     .map(|t| t.as_ref().into())
        //     .collect::<Vec<graphs_ai::model::Tool>>();
        // let model_tools_json = serde_json::to_string(&model_tools).unwrap();
        // info!("tools: {model_tools_json}");
        tools
    };

    // {
    //     let weather_tool: Box<dyn Tool> = Box::new(WeatherTool::new());
    //     let tool: graphs_ai::model::Tool = weather_tool.as_ref().into();
    //     let tool_json = serde_json::to_string(&tool).unwrap();
    //     info!("weather tool: {tool_json}");
    // }

    // read the env var with the api key
    let api_key = std::env::var("LLM_API_KEY").unwrap();
    let base_url = std::env::var("LLM_BASE_URL").unwrap();
    let model_name = std::env::var("MODEL_NAME").unwrap();

    let model = OpenAIModel::new(api_key, &model_name, base_url);

    let tools = {
        let local_tools: Vec<Box<dyn Tool>> = vec![Box::new(WeatherTool::new())];

        mcp_tools
            .into_iter()
            .chain(local_tools)
            .collect::<Vec<Box<dyn Tool>>>()
    };

    let tool_descriptions = tools
        .iter()
        .map(|tool| tool.get_full_description())
        .collect::<Vec<ToolDescription>>();

    let mut graph = graphs::Graph::new();

    let remove_system_prompt_id = graph.register_node(remove_system_prompt());

    graph
        .start()
        .then(user_input_node())
        // .then(remove_system_prompt())
        .then(remove_system_prompt_id)
        .then(add_system_prompt(
            "You are a helpful assistant. Do your best to help the user.",
            SystemPromptLocation::FirstMessage,
        ))
        .then(agent_node(model_name, Box::new(model), tool_descriptions))
        .branch(
            response_has_tool_node(),
            |graph| {
                graph.then(invoke_tool(tools)).then(remove_system_prompt_id); // loop back
            },
            |graph| graph.terminate(),
        );

    let runner = GraphRunner::new(graph);

    let mut state = ConversationState::new();

    loop {
        state = runner.run(state);
        let json_state = serde_json::to_string_pretty(&state).unwrap();
        info!("next state: {json_state}");

        let last_output = &state
            .messages()
            .last()
            .expect("expected at least one message")
            .content;

        info!("output: {last_output}");
    }
}

#[cfg(test)]
mod tests {
    use graphs::{Action, Graph, GraphRunner};

    fn adder(add: i32) -> Action<i32> {
        Action::new("adder", Box::new(move |x| x + add))
    }

    fn subtractor(subtract: i32) -> Action<i32> {
        Action::new("subtractor", Box::new(move |x| x - subtract))
    }

    fn multiplier(multiply: i32) -> Action<i32> {
        Action::new("multiplier", Box::new(move |x| x * multiply))
    }

    #[test]
    fn one_plus_one() {
        let mut graph = Graph::new();

        graph
            .start()
            .then(adder(1))
            .then(adder(2))
            .then(multiplier(3))
            .terminate();

        let runner = GraphRunner::new(graph);

        // 3 + 1 + 1 + 2 * 3 = 21
        let result = runner.run(3);

        assert_eq!(result, 18);
    }
}
