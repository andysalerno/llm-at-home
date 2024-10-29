use crate::cell::Cell;

pub struct WhileCell<T> {
    condition: Box<dyn Condition<T>>,
    body: Box<dyn Cell<T>>,
}

pub trait Condition<T> {
    fn evaluate(&self, input: &T) -> bool;
}

pub struct AlwaysTrueCondition;

impl<T> Condition<T> for AlwaysTrueCondition {
    fn evaluate(&self, _: &T) -> bool {
        true
    }
}

pub struct AlwaysFalseCondition;

impl<T> Condition<T> for AlwaysFalseCondition {
    fn evaluate(&self, _: &T) -> bool {
        false
    }
}

impl<T: Clone> Cell<T> for WhileCell<T> {
    fn run(&self, input: &T) -> T {
        let mut result = input.clone();
        while self.condition.evaluate(&result) {
            result = self.body.run(&result);
        }

        result
    }
}
