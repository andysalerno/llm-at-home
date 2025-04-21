use log::info;

// The NodeId can only be created internally by the graph structure,
// so it should be impossible to ever have a NodeId handle that cannot be resolved to its Node,
// as long as the graph structure does its job. (Nodes are never removed, only created)
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct NodeId(usize);

pub struct Action<T> {
    // TODO: add concept of 'preconditions' which are verified before the action is invoked
    // i.e. 'the last message of the state must have role user'
    action: Box<dyn Fn(T) -> T>,
}

impl<T> Action<T> {
    pub fn new(action: Box<dyn Fn(T) -> T>) -> Self {
        Self { action }
    }
}

pub struct Condition<T> {
    condition: Box<dyn Fn(&T) -> bool>,
}

impl<T> Condition<T> {
    pub fn new(condition: Box<dyn Fn(&T) -> bool>) -> Self {
        Self { condition }
    }
}

pub enum Node<T> {
    Action(Action<T>),
    Branch(Condition<T>),
    Terminal,
}

struct IdentifiedNode<T> {
    id: NodeId,
    node: Node<T>,
}

impl<T> From<Action<T>> for Node<T> {
    fn from(action: Action<T>) -> Self {
        Self::Action(action)
    }
}

impl<T> From<Condition<T>> for Node<T> {
    fn from(action: Condition<T>) -> Self {
        Self::Branch(action)
    }
}

struct Edge {
    from: NodeId,
    to: NodeId,
}

struct BranchEdge {
    from: NodeId,
    to_when_true: NodeId,
    to_when_false: NodeId,
}

pub struct Graph<T> {
    nodes: Vec<IdentifiedNode<T>>,
    edges: Vec<Edge>,
    condition_edges: Vec<BranchEdge>,
    start_id: NodeId,
}

fn no_op_start_node<T>() -> Action<T> {
    Action::new(Box::new(|x| x))
}

impl<T> Graph<T> {
    pub fn new() -> Self {
        Graph {
            nodes: Vec::new(),
            edges: Vec::new(),
            condition_edges: Vec::new(),
            start_id: NodeId(0),
        }
    }

    pub fn start(&mut self) -> GraphAdding<'_, T> {
        let start_id = self.register_node(no_op_start_node());

        GraphAdding {
            graph: self,
            last_added: start_id,
        }
    }

    pub const fn start_id(&self) -> NodeId {
        self.start_id
    }

    pub fn add_node_from(&mut self, from: NodeId, node: impl Into<Node<T>>) -> GraphAdding<'_, T> {
        let node = node.into();
        let next_id = self.register_node(node);
        self.edges.push(Edge { from, to: next_id });

        GraphAdding {
            graph: self,
            last_added: next_id,
        }
    }

    fn register_node(&mut self, node: impl Into<Node<T>>) -> NodeId {
        let node = node.into();
        let next_id = NodeId(self.nodes.len());
        self.nodes.push(IdentifiedNode { id: next_id, node });

        next_id
    }

    fn add_node(&mut self, node: impl Into<Node<T>>) -> GraphAdding<'_, T> {
        let node_id = self.register_node(node);

        GraphAdding {
            graph: self,
            last_added: node_id,
        }
    }

    fn add_edge(&mut self, edge: Edge) {
        self.edges.push(edge);
    }

    fn make_terminal(&mut self, node_id: NodeId) {
        let terminal_node_id = self.register_node(Node::Terminal);

        // Add an edge from the node to the terminal node:
        self.edges.push(Edge {
            from: node_id,
            to: terminal_node_id,
        });
    }

    fn next_nodes(&self, node_id: NodeId) -> Vec<NodeId> {
        self.edges
            .iter()
            .filter(|edge| edge.from == node_id)
            .map(|edge| edge.to)
            .collect()
    }

    fn next_nodes_for_condition(&self, condition_node: NodeId) -> Vec<(NodeId, NodeId)> {
        self.condition_edges
            .iter()
            .filter(|edge| edge.from == condition_node)
            .map(|edge| (edge.to_when_true, edge.to_when_false))
            .collect()
    }

    fn node(&self, node_id: NodeId) -> &Node<T> {
        &self.nodes.get(node_id.0).expect("Expected a node").node
    }
}

pub struct GraphAdding<'a, T> {
    graph: &'a mut Graph<T>,
    last_added: NodeId,
}

impl<'a, T> GraphAdding<'a, T> {
    #[must_use]
    pub fn then(self, node: Action<T>) -> Self {
        let next_id = self.graph.register_node(node);
        self.graph.edges.push(Edge {
            from: self.last_added,
            to: next_id,
        });

        Self {
            graph: self.graph,
            last_added: next_id,
        }
    }

    pub fn branch(
        self,
        condition: Condition<T>,
        branch_when_true: impl FnOnce(GraphAdding<'_, T>),
        branch_when_false: impl FnOnce(GraphAdding<'_, T>),
    ) {
        let condition_node_id = self.graph.register_node(condition);
        self.graph.edges.push(Edge {
            from: self.last_added,
            to: condition_node_id,
        });

        let graph = self.graph;

        // Reborrow graph for the true branch
        branch_when_true(GraphAdding {
            graph,
            last_added: condition_node_id,
        });

        // Reborrow graph again for the false branch
        branch_when_false(GraphAdding {
            graph,
            last_added: condition_node_id,
        });
    }

    pub fn terminate(self) {
        self.graph.make_terminal(self.last_added);
    }
}

pub struct GraphRunner<T> {
    graph: Graph<T>,
}

impl<T> GraphRunner<T> {
    #[must_use]
    pub const fn new(graph: Graph<T>) -> Self {
        Self { graph }
    }

    pub fn run(&self, input: T) -> T {
        let mut result = input;

        let mut cur_node_id = self.graph.start_id;

        loop {
            let cur_node = self.graph.node(cur_node_id);

            info!("Current node: {cur_node_id:?}");

            match cur_node {
                Node::Action(action) => {
                    result = (action.action)(result);
                    cur_node_id = *self.graph.next_nodes(cur_node_id).first().unwrap();
                }
                Node::Branch(condition) => {
                    let next_nodes = self.graph.next_nodes_for_condition(cur_node_id);
                    if (condition.condition)(&result) {
                        cur_node_id = next_nodes.first().unwrap().0;
                    } else {
                        cur_node_id = next_nodes.first().unwrap().1;
                    }
                }
                Node::Terminal => return result,
            }
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn adder(add: i32) -> Action<i32> {
        Action {
            action: Box::new(move |x| x + add),
        }
    }

    fn subtractor(subtract: i32) -> Action<i32> {
        Action {
            action: Box::new(move |x| x - subtract),
        }
    }

    fn multiplier(multiply: i32) -> Action<i32> {
        Action {
            action: Box::new(move |x| x * multiply),
        }
    }

    #[test]
    fn one_plus_one() {
        let mut graph = Graph::new();

        graph
            .start()
            .then(adder(1))
            .then(adder(2))
            .then(multiplier(3))
            .branch(
                Condition {
                    condition: Box::new(|x| *x > 10),
                },
                |graph| graph.then(adder(2)).terminate(),
                |graph| graph.then(subtractor(1)).terminate(),
            );

        let runner = GraphRunner::new(graph);

        // 3 + 1 + 1 + 2 * 3 = 21
        let result = runner.run(3);

        assert_eq!(result, 18);
    }
}
