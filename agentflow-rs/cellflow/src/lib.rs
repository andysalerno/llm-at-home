pub mod cell;
pub mod ifcell;
pub mod runner;
pub mod sequencecell;
pub mod whilecell;

#[cfg(test)]
mod tests {
    use crate::{
        cell::{Cell, NoOpCell},
        ifcell::IfCell,
        runner::run,
        sequencecell::SequenceCell,
        whilecell::Condition,
    };

    struct AdditionCell {
        increment: usize,
    }

    impl AdditionCell {
        const fn new(increment: usize) -> Self {
            Self { increment }
        }
    }

    impl Cell<usize> for AdditionCell {
        fn run(&self, input: &usize) -> usize {
            input + self.increment
        }
    }

    struct GreaterThanCondition {
        value: usize,
    }

    impl Condition<usize> for GreaterThanCondition {
        fn evaluate(&self, input: &usize) -> bool {
            *input > self.value
        }
    }

    #[test]
    fn trivial_sequence_1() {
        let seq = SequenceCell::new(vec![Box::new(AdditionCell::new(1))]);

        let output = run(&seq, &0);

        assert_eq!(1, output);
    }

    #[test]
    fn trivial_sequence_2() {
        let seq = SequenceCell::new(vec![
            Box::new(AdditionCell::new(2)),
            Box::new(AdditionCell::new(2)),
            Box::new(AdditionCell::new(1)),
        ]);

        let output = run(&seq, &0);

        assert_eq!(5, output);
    }

    #[test]
    fn trivial_sequence_3() {
        let seq = SequenceCell::new(vec![
            Box::new(AdditionCell::new(2)),
            Box::new(AdditionCell::new(2)),
            Box::new(AdditionCell::new(1)),
            Box::new(IfCell::new(
                Box::new(GreaterThanCondition { value: 42 }),
                Box::new(AdditionCell::new(7)),
                Box::new(AdditionCell::new(11)),
            )),
        ]);

        let output = run(&seq, &0);

        assert_eq!(16, output);
    }
}
