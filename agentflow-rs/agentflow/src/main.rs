use agent::Agent;
use cellflow::{Cell, CellVisitor, CustomCell, Handler, SequenceCell};
use cells::{AgentCellConfig, AgentCellHandler};
use conversation::ConversationState;

mod agent;
mod cells;
mod conversation;

fn main() {
    let program: Cell = SequenceCell::new(vec![CustomCell::new(
        AgentCellHandler::name(),
        AgentCellConfig::new("WebAgent".into()),
    )
    .into()])
    .into();

    let handlers = vec![Handler::from_cell_handler(AgentCellHandler::new_owned(
        DummyAgent,
    ))];

    let visitor = CellVisitor::new(handlers);

    let output = visitor.run(&program, &ConversationState::empty());
}

struct DummyAgent;

impl Agent for DummyAgent {
    fn name(&self) -> conversation::AgentName {
        todo!()
    }

    fn role(&self) -> conversation::Role {
        todo!()
    }

    fn behavior(&self) -> cellflow::Cell {
        todo!()
    }
}
