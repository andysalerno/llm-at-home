use crate::{AgentName, ConversationState, Message, Role};
use cellflow::{CellHandler, CellHandlerConfig, CellVisitor, Id};
use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize, Clone, Debug)]
pub struct ConsoleInputCellConfig {
    id: Id,
}

impl ConsoleInputCellConfig {
    #[must_use]
    pub fn new() -> Self {
        Self {
            id: Id::new("test"),
        }
    }
}

impl CellHandlerConfig for ConsoleInputCellConfig {
    fn cell_type_id() -> Id {
        Id::new("agent-cell")
    }

    fn instance_id(&self) -> Id {
        self.id.clone()
    }
}

#[derive(Debug)]
pub struct ConsoleInputCellHandler {
    id: Id,
}

impl ConsoleInputCellHandler {
    pub fn new() -> Self {
        Self {
            id: Id::new("test"),
        }
    }
}

impl CellHandler<ConversationState> for ConsoleInputCellHandler {
    type Config = ConsoleInputCellConfig;

    fn evaluate(
        &self,
        item: &ConversationState,
        _cell_config: &Self::Config,
        _visitor: &CellVisitor<ConversationState>,
    ) -> ConversationState {
        let input = {
            println!("User input: ");
            let mut input = String::new();
            std::io::stdin().read_line(&mut input).unwrap();
            input
        };

        let mut item = item.clone();

        item.add_message(Message::new(
            AgentName::new("User"),
            Role::new("User"),
            input,
        ));

        item.clone()
    }

    fn instance_id(&self) -> Id {
        self.id.clone()
    }
}
