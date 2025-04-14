fn main() {
    println!("Hello, world!");
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
        let mut graph = Graph::start(adder(1));
        let start_id = graph.start_id();

        graph
            .add_node_from(start_id, adder(1))
            .then(adder(2))
            .then(multiplier(3))
            .terminate();

        let runner = GraphRunner::new(graph);

        // 3 + 1 + 1 + 2 * 3 = 21
        let result = runner.run(3);

        assert_eq!(result, 21);
    }
}
