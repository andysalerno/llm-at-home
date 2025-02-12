use crate::{agent::Agent, conversation::ConversationState};
use cellflow::{CellHandler, CellHandlerConfig, CellVisitor, Id};
use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize, Clone, Debug)]
pub struct AgentCellConfig {
    name: String,
    agent_id: Id,
}

impl AgentCellConfig {
    #[must_use]
    pub const fn new(name: String, agent_id: Id) -> Self {
        Self { name, agent_id }
    }
}

impl CellHandlerConfig for AgentCellConfig {
    fn cell_type_id() -> Id {
        Id::new("agent-cell")
    }

    fn instance_id(&self) -> Id {
        self.agent_id.clone()
    }
}

#[derive(Debug)]
pub struct AgentCellHandler {
    agent: Box<dyn Agent>,
    agent_id: Id,
}

impl AgentCellHandler {
    pub fn new<T>(value: T, agent_id: Id) -> Self
    where
        T: Into<Box<dyn Agent>>,
    {
        Self {
            agent: value.into(),
            agent_id,
        }
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

    fn instance_id(&self) -> Id {
        self.agent_id.clone()
    }
}
