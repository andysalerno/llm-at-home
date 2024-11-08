use agent::Agent;
use cellflow::{CellVisitor, CustomCell, Handler, SequenceCell};
use cells::{AgentCell, AgentCellConfig};
use conversation::ConversationState;

mod agent;
mod cells;
mod conversation;

fn main() {
    let program = SequenceCell::new(vec![CustomCell::new(
        AgentCell::name(),
        AgentCellConfig::new("WebAgent".into()),
    )
    .into()]);

    let visitor = CellVisitor::new(vec![Handler::Cell(Box::new(AgentCell::new(Box::new(
        DummyAgent,
    ))))]);

    let output = visitor.run(&program.into(), &ConversationState::empty());
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
