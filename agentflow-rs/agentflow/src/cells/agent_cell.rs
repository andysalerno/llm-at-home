use std::cell::Cell;

use cellflow::{CellHandler, CellVisitor, Id};
use serde::{Deserialize, Serialize};

use crate::{agent::Agent, conversation::ConversationState};

#[derive(Serialize, Deserialize, Clone, Debug)]
pub struct AgentCellConfig {
    name: String,
}

impl AgentCellConfig {
    pub const fn new(name: String) -> Self {
        Self { name }
    }
}

pub struct AgentCellHandler {
    agent: Box<dyn Agent>,
}

impl AgentCellHandler {
    pub fn new(agent: Box<dyn Agent>) -> Self {
        Self { agent }
    }

    pub fn name() -> Id {
        Id::new("agent_cell".into())
    }
}

impl CellHandler<ConversationState> for AgentCellHandler {
    type Config = AgentCellConfig;

    fn name(&self) -> cellflow::Id {
        Self::name()
    }

    fn evaluate(
        &self,
        item: &ConversationState,
        cell_config: &Self::Config,
        visitor: &CellVisitor<ConversationState>,
    ) -> ConversationState {
        let program = self.agent.behavior();

        visitor.run(&program, item)
    }
}

// impl AgentCell {
//     pub fn into_cell(self) -> CustomCell {
//         CustomCell::new()
//     }
// }
