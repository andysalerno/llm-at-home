use crate::{agent::Agent, conversation::ConversationState};
use cellflow::{CellHandler, CellHandlerConfig, CellVisitor, Id};
use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize, Clone, Debug)]
pub struct AgentCellConfig {
    name: String,
}

impl AgentCellConfig {
    #[must_use]
    pub const fn new(name: String) -> Self {
        Self { name }
    }
}

impl CellHandlerConfig for AgentCellConfig {
    fn id() -> Id {
        Id::new("agent-cell")
    }
}

pub struct AgentCellHandler {
    agent: Box<dyn Agent>,
}

impl AgentCellHandler {
    pub fn new<T>(value: T) -> Self
    where
        T: Into<Box<dyn Agent>>,
    {
        Self {
            agent: value.into(),
        }
    }

    #[must_use]
    pub fn id() -> Id {
        AgentCellConfig::id()
    }
}

impl CellHandler<ConversationState> for AgentCellHandler {
    type Config = AgentCellConfig;

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
