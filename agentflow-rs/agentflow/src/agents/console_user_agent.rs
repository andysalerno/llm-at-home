use cellflow::{CellHandler, CellHandlerConfig, CustomCell, Id};
use serde::{Deserialize, Serialize};

use crate::{Agent, AgentName, ConversationState, Message, Role};

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
        _: &cellflow::CellVisitor<ConversationState>,
    ) -> ConversationState {
        let user_input = "some user input";

        let mut next_state = item.clone();

        next_state.add_message(Message::new(
            cell_config.agent_name.clone(),
            cell_config.role.clone(),
            user_input.into(),
        ));

        next_state
    }
}
