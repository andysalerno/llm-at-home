use serde::Deserialize;

use crate::{cell::Cell, Id, Json};

/// A trait representing a handler for a type of cell.
/// It maps to the cell type by id.
pub trait CellHandler<T> {
    fn id(&self) -> Id;
    fn handle(&self, item: &T) -> T;
}

pub trait ConditionEvaluatorInner<T> {
    fn id(&self) -> Id;
    fn evaluate(&self, item: &T, condition_body: &Json) -> bool;
}

pub trait ConditionEvaluator<T> {
    type Body: for<'a> Deserialize<'a>;
    fn id(&self) -> Id;
    fn evaluate(&self, item: &T, condition_body: &Self::Body) -> bool;
}

impl<T, TItem> ConditionEvaluatorInner<TItem> for T
where
    T: ConditionEvaluator<TItem>,
{
    fn id(&self) -> Id {
        ConditionEvaluator::id(self)
    }

    fn evaluate(&self, item: &TItem, condition_body: &Json) -> bool {
        let parsed: T::Body = serde_json::from_value(condition_body.0.clone()).unwrap();

        ConditionEvaluator::evaluate(self, item, &parsed)
    }
}

pub enum Handler<T> {
    Cell(Box<dyn CellHandler<T>>),
    Condition(Box<dyn ConditionEvaluatorInner<T>>),
}

impl<T> From<Box<dyn ConditionEvaluatorInner<T>>> for Handler<T> {
    fn from(v: Box<dyn ConditionEvaluatorInner<T>>) -> Self {
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
                let condition_body = if_cell.condition().body();

                if condition_handler.evaluate(input, condition_body) {
                    self.visit(if_cell.on_true(), input)
                } else {
                    self.visit(if_cell.on_false(), input)
                }
            }
            Cell::While(while_cell) => {
                let condition_handler = self.select_condition(while_cell.condition().id());
                let condition_body = while_cell.condition().body();

                let mut result = input.clone();

                while condition_handler.evaluate(input, condition_body) {
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
            Cell::NoOp => input.clone(),
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

    fn select_condition(&self, id: &Id) -> &dyn ConditionEvaluatorInner<T> {
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
    use serde::{Deserialize, Serialize};

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

    #[derive(Serialize, Deserialize)]
    struct GreaterThanCondition(usize);

    impl GreaterThanCondition {
        fn id() -> Id {
            Id::new("greater-than-condition".into())
        }
    }

    struct GreaterThanConditionEvaluator;

    impl ConditionEvaluator<MyState> for GreaterThanConditionEvaluator {
        type Body = GreaterThanCondition;
        fn id(&self) -> Id {
            GreaterThanCondition::id()
        }

        fn evaluate(&self, item: &MyState, condition: &GreaterThanCondition) -> bool {
            item.0 > condition.0
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
                Condition::new(GreaterThanCondition::id(), GreaterThanCondition(7)),
                Box::new(Cell::Custom(Incrementor::id())),
                Box::new(Cell::NoOp),
            )),
            Cell::Custom(Incrementor::id()),
        ]);

        let visitor = CellVisitor::new(vec![
            Handler::Cell(Box::new(Incrementor)),
            Handler::Condition(Box::new(GreaterThanConditionEvaluator)),
        ]);

        let output = visitor.run(&program.into(), &MyState(5));

        assert_eq!(8, output.0);
    }

    #[test]
    fn test_simple_program_4() {
        let program = SequenceCell::new(vec![
            Cell::Custom(Incrementor::id()),
            Cell::Custom(Incrementor::id()),
            Cell::If(IfCell::new(
                Condition::new(GreaterThanCondition::id(), GreaterThanCondition(7)),
                Box::new(Cell::NoOp),
                Box::new(Cell::Custom(Incrementor::id())),
            )),
            Cell::Custom(Incrementor::id()),
        ]);

        let visitor = CellVisitor::new(vec![
            Handler::Cell(Box::new(Incrementor)),
            Handler::Condition(Box::new(GreaterThanConditionEvaluator)),
        ]);

        let output = visitor.run(&program.into(), &MyState(5));

        assert_eq!(9, output.0);
    }

    #[test]
    fn test_serialize() {
        let condition = Condition::new(GreaterThanCondition::id(), GreaterThanCondition(7));

        let if_cell = Cell::If(IfCell::new(
            condition,
            Box::new(Cell::NoOp),
            Box::new(Cell::NoOp),
        ));

        let cell = Cell::Sequence(SequenceCell::new(vec![if_cell]));

        let serialized = serde_json::to_string(&cell).unwrap();

        assert_eq!(
            "{\"Sequence\":{\"sequence\":[{\"If\":{\"condition\":{\"id\":\"greater-than-condition\",\"body\":7},\"on_true\":\"NoOp\",\"on_false\":\"NoOp\"}}]}}",
            &serialized);
    }

    #[test]
    fn test_deserialize() {
        let json = 
            "{\"Sequence\":{\"sequence\":[{\"If\":{\"condition\":{\"id\":\"greater-than-condition\",\"body\":7},\"on_true\":\"NoOp\",\"on_false\":\"NoOp\"}}]}}";

        let deserialized: Cell = serde_json::from_str(json).unwrap();

        assert!(matches!(deserialized, Cell::Sequence(_)));
    }
}
