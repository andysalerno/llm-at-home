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

    let visitor = CellVisitor::new(vec![Handler::Cell(Box::new(AgentCell::new()))]);

    let output = visitor.run(&program.into(), &ConversationState::new());
}
