use crate::Agent;

#[derive(Debug)]
pub struct ConsoleUserAgent;

impl Agent for ConsoleUserAgent {
    fn name(&self) -> crate::AgentName {
        todo!()
    }

    fn role(&self) -> crate::Role {
        todo!()
    }

    fn behavior(&self) -> cellflow::Cell {
        todo!()
    }
}
