use crate::conversation::{AgentName, Role};
use cellflow::Cell;

/// An `Agent` has some behavior that can be executed in a conversation.
pub trait Agent: std::fmt::Debug {
    fn name(&self) -> AgentName;
    fn role(&self) -> Role;
    fn behavior(&self) -> Cell;
}

impl<T> From<T> for Box<dyn Agent>
where
    T: Agent + 'static,
{
    fn from(value: T) -> Self {
        Box::new(value)
    }
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
