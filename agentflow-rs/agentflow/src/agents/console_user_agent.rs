use crate::{cells::ConsoleInputCellConfig, Agent, AgentName, Role};

#[derive(Debug)]
pub struct ConsoleUserAgent {}

impl ConsoleUserAgent {
    pub fn new() -> Self {
        Self {}
    }
}

impl Agent for ConsoleUserAgent {
    fn name(&self) -> AgentName {
        todo!()
    }

    fn role(&self) -> Role {
        todo!()
    }

    fn behavior(&self) -> cellflow::Cell {
        ConsoleInputCellConfig::new().into()
    }
}
