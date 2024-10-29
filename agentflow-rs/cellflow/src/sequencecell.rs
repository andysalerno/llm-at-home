use crate::cell::Cell;

pub struct SequenceCell<T> {
    sequence: Vec<Box<dyn Cell<T>>>,
}

impl<T> SequenceCell<T> {
    #[must_use]
    pub fn new(sequence: Vec<Box<dyn Cell<T>>>) -> Self {
        Self { sequence }
    }
}

impl<T: Clone> Cell<T> for SequenceCell<T> {
    fn run(&self, input: &T) -> T {
        let mut cur = input.clone();

        for cell in &self.sequence {
            cur = cell.run(&cur);
        }

        cur
    }
}
