use std::cell::Cell;

use cellflow::{CellHandler, CellHandlerConfig, CustomCell, Id};
use serde::{Deserialize, Serialize};

use crate::{
    cells::{ConsoleInputCellConfig, ConsoleInputCellHandler},
    Agent, AgentName, ConversationState, Message, Role,
};

#[derive(Debug)]
pub struct ConsoleUserAgent {
    agent_name: AgentName,
    role: Role,
}

impl ConsoleUserAgent {
    #[must_use]
    pub const fn new(agent_name: AgentName, role: Role) -> Self {
        Self { agent_name, role }
    }
}

impl Agent for ConsoleUserAgent {
    fn name(&self) -> crate::AgentName {
        self.agent_name.clone()
    }

    fn role(&self) -> crate::Role {
        self.role.clone()
    }

    fn behavior(&self) -> cellflow::Cell {
        let config = GetUserConsoleInputCellConfig::new(self.name(), self.role());
        CustomCell::new(config).into()
    }
}

#[derive(Debug, Serialize, Deserialize)]
pub struct GetUserConsoleInputCellConfig {
    agent_name: AgentName,
    role: Role,
}

impl GetUserConsoleInputCellConfig {
    #[must_use]
    pub const fn new(agent_name: AgentName, role: Role) -> Self {
        Self { agent_name, role }
    }
}

impl CellHandlerConfig for GetUserConsoleInputCellConfig {
    fn cell_type_id() -> cellflow::Id {
        Id::new("GetUserConsoleInputCell")
    }
}

#[derive(Debug)]
pub struct GetUserConsoleInputCellHandler;

impl CellHandler<ConversationState> for GetUserConsoleInputCellHandler {
    type Config = GetUserConsoleInputCellConfig;

    fn evaluate(
        &self,
        item: &ConversationState,
        cell_config: &Self::Config,
        visitor: &cellflow::CellVisitor<ConversationState>,
    ) -> ConversationState {
        let cell = CustomCell::new(ConsoleInputCellConfig::new());

        visitor.run(&cell.into(), item)
    }
}
