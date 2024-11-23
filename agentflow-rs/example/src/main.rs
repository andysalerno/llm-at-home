use agentflow::{
    Agent, AgentCellConfig, AgentCellHandler, AgentName, CellHandler, ConversationState, Role,
};
use cellflow::{Cell, CellVisitor, CustomCell, SequenceCell};

fn main() {
    let program: Cell = SequenceCell::new(vec![CustomCell::new(
        AgentCellHandler::name(),
        AgentCellConfig::new("WebAgent".into()),
    )
    .into()])
    .into();

    let handlers = vec![AgentCellHandler::new(DummyAgent).into_handler()];

    let visitor = CellVisitor::new(handlers);

    let output = visitor.run(&program, &ConversationState::empty());
}

struct DummyAgent;

impl Agent for DummyAgent {
    fn name(&self) -> AgentName {
        todo!()
    }

    fn role(&self) -> Role {
        todo!()
    }

    fn behavior(&self) -> cellflow::Cell {
        todo!()
    }
}
