use cellflow::{CellHandler, Id};
use serde::{Deserialize, Serialize};

use crate::conversation::ConversationState;

#[derive(Serialize, Deserialize)]
pub struct AgentCellConfig {
    name: String,
}

impl AgentCellConfig {
    pub fn new(name: String) -> Self {
        Self { name }
    }
}

pub struct AgentCell;

impl AgentCell {
    pub fn name() -> Id {
        Id::new("agent_cell".into())
    }

    pub fn new() -> Self {
        AgentCell
    }
}

impl CellHandler<ConversationState> for AgentCell {
    type Config = AgentCellConfig;

    fn name(&self) -> cellflow::Id {
        Self::name()
    }

    fn evaluate(&self, item: &ConversationState, cell_config: &Self::Config) -> ConversationState {
        item.clone()
    }
}

// impl AgentCell {
//     pub fn into_cell(self) -> CustomCell {
//         CustomCell::new()
//     }
// }
