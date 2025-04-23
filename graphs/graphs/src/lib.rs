use std::fmt::Debug;

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
    display_name: String,
}

impl<T> Debug for Action<T> {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        f.debug_struct("Action")
            .field("display_name", &self.display_name)
            .finish()
    }
}

impl<T> Action<T> {
    pub fn new(display_name: impl Into<String>, action: Box<dyn Fn(T) -> T>) -> Self {
        Self {
            action,
            display_name: display_name.into(),
        }
    }
}

pub struct Condition<T> {
    condition: Box<dyn Fn(&T) -> bool>,
    display_name: String,
}

impl<T> Debug for Condition<T> {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        f.debug_struct("Condition")
            .field("display_name", &self.display_name)
            .finish()
    }
}

impl<T> Condition<T> {
    pub fn new(display_name: impl Into<String>, condition: Box<dyn Fn(&T) -> bool>) -> Self {
        Self {
            condition,
            display_name: display_name.into(),
        }
    }
}

#[derive(Debug)]
pub enum Node<T> {
    Action(Action<T>),
    Branch(Condition<T>),
    Terminal,
}

impl<T> Node<T> {
    pub fn display_name(&self) -> &str {
        match self {
            Self::Action(action) => &action.display_name,
            Self::Branch(condition) => &condition.display_name,
            Self::Terminal => "Terminal",
        }
    }
}

#[derive(Debug)]
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

#[derive(Debug)]
struct Edge {
    from: NodeId,
    to: NodeId,
}

#[derive(Clone, Debug)]
struct BranchEdge {
    from: NodeId,
    to_when_true: NodeId,
    to_when_false: NodeId,
}

#[derive(Debug)]
struct IdGenerator {
    next_id: usize,
}

impl IdGenerator {
    fn new() -> Self {
        Self { next_id: 0 }
    }

    fn next_id(&mut self) -> NodeId {
        let id = self.next_id;
        self.next_id += 1;
        NodeId(id)
    }
}

#[derive(Debug)]
pub struct Graph<T> {
    nodes: Vec<IdentifiedNode<T>>,
    edges: Vec<Edge>,
    condition_edges: Vec<BranchEdge>,
    id_generator: IdGenerator,
    start_id: NodeId,
}

fn no_op_start_node<T>() -> Action<T> {
    Action::new("START", Box::new(|x| x))
}

impl<T> Graph<T> {
    pub fn new() -> Self {
        let mut id_generator = IdGenerator::new();
        let start_id = id_generator.next_id();

        Self {
            nodes: Vec::new(),
            edges: Vec::new(),
            condition_edges: Vec::new(),
            id_generator,
            start_id,
        }
    }

    pub fn start(&mut self) -> GraphAdding<'_, T> {
        let start_id = self.register_node(no_op_start_node());

        GraphAdding {
            graph: self,
            last_added: start_id,
        }
    }

    pub fn start_id(&self) -> NodeId {
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

    pub fn register_node(&mut self, node: impl Into<Node<T>>) -> NodeId {
        let node = node.into();

        // + 1 to avoid conflict with START node which is always 0
        let next_id = self.id_generator.next_id();
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

    fn edges_from(&self, node_id: NodeId) -> impl Iterator<Item = &Edge> {
        self.edges.iter().filter(move |edge| edge.from == node_id)
    }

    fn next_nodes(&self, node_id: NodeId) -> impl Iterator<Item = NodeId> {
        self.edges_from(node_id).map(|edge| edge.to)
    }

    fn node(&self, node_id: NodeId) -> &Node<T> {
        &self.nodes.get(node_id.0).expect("Expected a node").node
    }
}

impl<T> Default for Graph<T> {
    fn default() -> Self {
        Self::new()
    }
}

#[derive(Debug)]
pub struct GraphAdding<'a, T> {
    graph: &'a mut Graph<T>,
    last_added: NodeId,
}

pub enum Addable<T> {
    Action(Action<T>),
    ExistingNodeId(NodeId),
}

impl<T> From<NodeId> for Addable<T> {
    fn from(v: NodeId) -> Self {
        Self::ExistingNodeId(v)
    }
}

impl<T> From<Action<T>> for Addable<T> {
    fn from(v: Action<T>) -> Self {
        Self::Action(v)
    }
}

impl<T> GraphAdding<'_, T> {
    pub fn then(self, node: impl Into<Addable<T>>) -> Self {
        let addable = node.into();

        let next_node_id = match addable {
            Addable::Action(action) => self.graph.register_node(action),
            Addable::ExistingNodeId(node_id) => node_id,
        };

        let edge = Edge {
            from: self.last_added,
            to: next_node_id,
        };

        self.graph.edges.push(edge);

        Self {
            graph: self.graph,
            last_added: next_node_id,
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

        branch_when_true(GraphAdding {
            graph,
            last_added: condition_node_id,
        });

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

            info!(
                "Current node: {cur_node_id:?} name: {}",
                cur_node.display_name()
            );

            match cur_node {
                Node::Action(action) => {
                    result = (action.action)(result);

                    let mut next_nodes = self.graph.next_nodes(cur_node_id);
                    let next_node = next_nodes
                        .next()
                        .expect("Expected an action node to have one edge");

                    cur_node_id = next_node;

                    {
                        let next = next_nodes.next();
                        debug_assert!(next.is_none());
                    }
                }
                Node::Branch(condition) => {
                    let mut next_nodes = self.graph.next_nodes(cur_node_id);

                    // by convention, a branch has two edges, and the first one is the true branch
                    let true_node_id = next_nodes
                        .next()
                        .expect("Expected a branch to have at least one edge");

                    let false_node_id = next_nodes
                        .next()
                        .expect("Expected a branch to have at least two edges");

                    {
                        let next = next_nodes.next();
                        debug_assert!(next.is_none());
                    }

                    if (condition.condition)(&result) {
                        info!("Condition {} evaluated to true", condition.display_name);
                        cur_node_id = true_node_id;
                    } else {
                        info!("Condition {} evaluated to false", condition.display_name);
                        cur_node_id = false_node_id;
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
        Action::new("adder", Box::new(move |x| x + add))
    }

    fn subtractor(subtract: i32) -> Action<i32> {
        Action::new("subtractor", Box::new(move |x| x - subtract))
    }

    fn multiplier(multiply: i32) -> Action<i32> {
        Action::new("multiplier", Box::new(move |x| x * multiply))
    }

    #[test]
    fn one_plus_one_branching() {
        let mut graph = Graph::new();

        graph
            .start()
            .then(adder(1))
            .then(adder(2))
            .then(multiplier(3))
            .branch(
                Condition {
                    condition: Box::new(|&x| x > 10),
                    display_name: "is_greater_than_10".to_string(),
                },
                |graph| graph.then(adder(2)).terminate(),
                |graph| graph.then(subtractor(1)).terminate(),
            );

        let runner = GraphRunner::new(graph);

        let result = runner.run(3);

        assert_eq!(result, 20);
    }

    #[test]
    fn start_node_is_id_0() {
        let graph = Graph::<i32>::new();

        assert_eq!(graph.start_id(), NodeId(0));
    }

    #[test]
    fn first_node_is_not_id_0() {
        let mut graph = Graph::<i32>::new();

        let first_node = graph.register_node(adder(1));

        assert_ne!(first_node, NodeId(0));
    }
}
