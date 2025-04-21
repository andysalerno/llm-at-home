use graphs::GraphRunner;
use graphs_ai::{
    agent::agent_node, model_openai::OpenAIModel, state::ConversationState, user::user_input_node,
};
use log::info;

fn main() {
    env_logger::init();

    let model = OpenAIModel::new(
        "<replace>",
        "mistralai/mistral-small-3.1-24b-instruct",
        "https://openrouter.ai/api/v1",
    );

    let user_input_node = user_input_node();
    let agent_node = agent_node(Box::new(model), &[]);

    let mut graph = graphs::Graph::new();

    graph
        .start()
        .then(user_input_node)
        .then(agent_node)
        .terminate();

    let runner = GraphRunner::new(graph);

    let mut state = ConversationState::new();

    loop {
        state = runner.run(state);
        info!("next state: {state:?}");
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
