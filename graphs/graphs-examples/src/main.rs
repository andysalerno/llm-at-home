use graphs::GraphRunner;
use graphs_ai::{
    agent::agent_node, model_openai::OpenAIModel, state::ConversationState, user::user_input_node,
};

fn main() {
    let model = OpenAIModel::new("key", "name");

    let user_input_node = user_input_node();
    let agent_node = agent_node(Box::new(model), &[]);

    let mut graph = graphs::Graph::new();

    graph
        .start()
        .then(user_input_node)
        .then(agent_node)
        .terminate();

    let runner = GraphRunner::new(graph);

    let result = runner.run(ConversationState::new());
}

#[cfg(test)]
mod tests {
    use graphs::{Action, Graph, GraphRunner};

    fn adder(add: i32) -> Action<i32> {
        Action::new(Box::new(move |x| x + add))
    }

    fn subtractor(subtract: i32) -> Action<i32> {
        Action::new(Box::new(move |x| x - subtract))
    }

    fn multiplier(multiply: i32) -> Action<i32> {
        Action::new(Box::new(move |x| x * multiply))
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
