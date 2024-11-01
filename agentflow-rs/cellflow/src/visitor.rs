use crate::{cell::Cell, Id};

/// A trait representing a handler for a type of cell.
/// It maps to the cell type by id.
pub trait CellHandler<T> {
    fn id(&self) -> Id;
    fn handle(&self, item: &T) -> T;
}

pub trait ConditionEvaluator<T> {
    fn id(&self) -> Id;
    fn evaluate(&self, item: &T) -> bool;
}

pub enum Handler<T> {
    Cell(Box<dyn CellHandler<T>>),
    Condition(Box<dyn ConditionEvaluator<T>>),
}

impl<T> From<Box<dyn ConditionEvaluator<T>>> for Handler<T> {
    fn from(v: Box<dyn ConditionEvaluator<T>>) -> Self {
        Self::Condition(v)
    }
}

impl<T> From<Box<dyn CellHandler<T>>> for Handler<T> {
    fn from(v: Box<dyn CellHandler<T>>) -> Self {
        Self::Cell(v)
    }
}

/// A visitor or interpreter that can visit a cell
/// and perform the given handler for it.
pub struct CellVisitor<T> {
    handlers: Vec<Handler<T>>,
}

impl<T: Clone> CellVisitor<T> {
    #[must_use]
    pub const fn new(handlers: Vec<Handler<T>>) -> Self {
        Self { handlers }
    }

    pub fn run(&self, program: &Cell, input: &T) -> T {
        self.visit(program, input)
    }

    fn visit(&self, cell: &Cell, input: &T) -> T {
        match &cell {
            Cell::If(if_cell) => {
                let condition_handler = self.select_condition(if_cell.condition().id());

                if condition_handler.evaluate(input) {
                    self.visit(if_cell.on_true(), input)
                } else {
                    self.visit(if_cell.on_false(), input)
                }
            }
            Cell::While(while_cell) => {
                let condition_handler = self.select_condition(while_cell.condition().id());

                let mut result = input.clone();

                while condition_handler.evaluate(input) {
                    result = self.visit(while_cell.body(), &result);
                }

                result
            }
            Cell::Sequence(sequence_cell) => {
                let mut result = input.clone();
                for cell in sequence_cell.sequence() {
                    result = self.visit(cell, &result);
                }

                result
            }
            Cell::Custom(id) => {
                let custom_handler = self.select_handler(id);

                custom_handler.handle(input)
            }
        }
    }

    fn select_handler(&self, id: &Id) -> &dyn CellHandler<T> {
        let found = self
            .handlers
            .iter()
            .filter_map(|h| match h {
                Handler::Cell(handler) => Some(handler),
                Handler::Condition(_) => None,
            })
            .find(|h| h.id() == *id)
            .expect("expected a condition to be registered");

        found.as_ref()
    }

    fn select_condition(&self, id: &Id) -> &dyn ConditionEvaluator<T> {
        let found = self
            .handlers
            .iter()
            .filter_map(|h| match h {
                Handler::Cell(_) => None,
                Handler::Condition(handler) => Some(handler),
            })
            .find(|h| h.id() == *id)
            .expect("expected a condition to be registered");

        found.as_ref()
    }
}

#[cfg(test)]
mod tests {
    use crate::{visitor::Handler, Cell, Condition, Id, IfCell, SequenceCell};

    use super::{CellHandler, CellVisitor, ConditionEvaluator};

    #[derive(Debug, Clone)]
    struct MyState(usize);

    struct Incrementor;

    impl Incrementor {
        fn id() -> crate::Id {
            Id::new("incrementor".into())
        }
    }

    impl CellHandler<MyState> for Incrementor {
        fn id(&self) -> crate::Id {
            Self::id()
        }

        fn handle(&self, item: &MyState) -> MyState {
            MyState(item.0 + 1)
        }
    }

    struct GreaterThanCondition(usize);

    impl GreaterThanCondition {
        fn id() -> Id {
            Id::new("greater-than-condition".into())
        }
    }

    impl ConditionEvaluator<MyState> for GreaterThanCondition {
        fn id(&self) -> Id {
            Self::id()
        }

        fn evaluate(&self, item: &MyState) -> bool {
            item.0 > self.0
        }
    }

    #[test]
    #[should_panic(expected = "checking object safety")]
    fn verify_object_safety() {
        let _unused: Box<dyn CellHandler<usize>> = todo!("checking object safety");
    }

    #[test]
    fn test_simple_program_1() {
        let program = SequenceCell::new(vec![Cell::Custom(Incrementor::id())]);

        let visitor = CellVisitor::new(vec![Handler::Cell(Box::new(Incrementor))]);

        let output = visitor.run(&program.into(), &MyState(5));

        assert_eq!(6, output.0);
    }

    #[test]
    fn test_simple_program_2() {
        let program = SequenceCell::new(vec![
            Cell::Custom(Incrementor::id()),
            Cell::Custom(Incrementor::id()),
            Cell::Custom(Incrementor::id()),
        ]);

        let visitor = CellVisitor::new(vec![Handler::Cell(Box::new(Incrementor))]);

        let output = visitor.run(&program.into(), &MyState(5));

        assert_eq!(8, output.0);
    }

    #[test]
    fn test_simple_program_3() {
        let program = SequenceCell::new(vec![
            Cell::Custom(Incrementor::id()),
            Cell::Custom(Incrementor::id()),
            Cell::If(IfCell::new(
                Condition::new(GreaterThanCondition::id()),
                Box::new(Cell::Custom(Incrementor::id())),
                Box::new(Cell::Custom(Incrementor::id())),
            )),
            Cell::Custom(Incrementor::id()),
        ]);

        let visitor = CellVisitor::new(vec![
            Handler::Cell(Box::new(Incrementor)),
            Handler::Condition(Box::new(GreaterThanCondition(7))),
        ]);

        let output = visitor.run(&program.into(), &MyState(5));

        assert_eq!(8, output.0);
    }
}
