// The NodeId can only be created internally by the graph structure,
// so it should be impossible to ever have a NodeId handle that cannot be resolved to its Node,
// as long as the graph structure does its job. (Nodes are never removed, only created)
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct NodeId(usize);

pub struct Action<T> {
    action: Box<dyn Fn(T) -> T>,
}

impl<T> Action<T> {
    pub fn new(action: Box<dyn Fn(T) -> T>) -> Self {
        Self { action }
    }
}

struct Condition<T> {
    condition: Box<dyn Fn(&T) -> bool>,
}

enum Node<T> {
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

impl<T> Graph<T> {
    pub fn start(start_node: impl Into<Node<T>>) -> Self {
        let start_node = start_node.into();
        let start_id = NodeId(0);

        let start_node = IdentifiedNode {
            id: start_id,
            node: start_node,
        };

        let nodes = vec![start_node];

        Graph {
            nodes,
            edges: Vec::new(),
            condition_edges: Vec::new(),
            start_id,
        }
    }

    pub const fn start_id(&self) -> NodeId {
        self.start_id
    }

    fn register_node(&mut self, node: impl Into<Node<T>>) -> NodeId {
        let node = node.into();
        let next_id = NodeId(self.nodes.len());
        self.nodes.push(IdentifiedNode { id: next_id, node });

        next_id
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

    fn add_node(&mut self, node: impl Into<Node<T>>) -> GraphAdding<'_, T> {
        let node_id = self.register_node(node);

        GraphAdding {
            graph: self,
            last_added: node_id,
        }
    }

    fn add_conditional(
        &mut self,
        from: NodeId,
        to_when_true: impl Into<Node<T>>,
        to_when_false: impl Into<Node<T>>,
        condition: Condition<T>,
    ) -> (NodeId, NodeId, NodeId) {
        let conditional_node_id = self.register_node(Node::Branch(condition));

        // Add the edge on the 'from' to the condition node:
        self.edges.push(Edge {
            from,
            to: conditional_node_id,
        });

        // Add the true and false nodes:
        let to_when_true = self.register_node(to_when_true);
        let to_when_false = self.register_node(to_when_false);

        // Add the edges from the condition node to the true and false nodes:
        self.condition_edges.push(BranchEdge {
            from: conditional_node_id,
            to_when_true,
            to_when_false,
        });

        (conditional_node_id, to_when_true, to_when_false)
    }

    fn add_conditional_to_ids(
        &mut self,
        from: NodeId,
        to_when_true: NodeId,
        to_when_false: NodeId,
        condition: Condition<T>,
    ) -> NodeId {
        let next_id = self.register_node(Node::Branch(condition));

        self.condition_edges.push(BranchEdge {
            from,
            to_when_true,
            to_when_false,
        });

        next_id
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

    pub fn terminate(self) {
        self.graph.make_terminal(self.last_added);
    }
}

pub struct GraphRunner<T> {
    graph: Graph<T>,
}

impl<T> GraphRunner<T> {
    pub fn new(graph: Graph<T>) -> Self {
        Self { graph }
    }

    pub fn run(&self, input: T) -> T {
        let mut result = input;

        let mut cur_node_id = self.graph.start_id;

        loop {
            let cur_node = self.graph.node(cur_node_id);

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
