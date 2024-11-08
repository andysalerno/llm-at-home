use cellflow::Cell;

use crate::conversation::{AgentName, Role};

pub trait Agent {
    fn name(&self) -> AgentName;
    fn role(&self) -> Role;
    fn behavior(&self) -> Cell;
}

#[cfg(test)]
mod tests {
    use super::Agent;

    #[test]
    fn ensure_object_safety() {
        let _: Box<dyn Agent> = todo!("ensuring object safety");
    }
}
