use crate::{cell::Cell, whilecell::Condition};

pub struct IfCell<T> {
    condition: Box<dyn Condition<T>>,
    on_true: Box<dyn Cell<T>>,
    on_false: Box<dyn Cell<T>>,
}

impl<T> IfCell<T> {
    #[must_use]
    pub fn new(
        condition: Box<dyn Condition<T>>,
        on_true: Box<dyn Cell<T>>,
        on_false: Box<dyn Cell<T>>,
    ) -> Self {
        Self {
            condition,
            on_true,
            on_false,
        }
    }
}

impl<T: Clone> Cell<T> for IfCell<T> {
    fn run(&self, input: &T) -> T {
        if self.condition.evaluate(input) {
            self.on_true.run(input)
        } else {
            self.on_false.run(input)
        }
    }
}
