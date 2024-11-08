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
    #[should_panic(expected = "checking object safety")]
    fn verify_object_safety() {
        let _unused: Box<dyn Agent> = todo!("checking object safety");
    }
}
