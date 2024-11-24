use crate::{cell::Cell, CellHandlerInner, CustomCell, Id, Json};
use log::info;
use serde::Deserialize;

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

// TODO: I don't like having an enum that represents both... let's split this up later.
pub enum Handler<T> {
    Cell(Box<dyn CellHandlerInner<T>>),
    Condition(Box<dyn ConditionEvaluatorInner<T>>),
}

impl<T> Handler<T> {
    pub fn from_cell_handler(cell_handler: impl CellHandlerInner<T> + 'static) -> Self {
        Self::Cell(Box::new(cell_handler))
    }
}

impl<T> From<Box<dyn ConditionEvaluatorInner<T>>> for Handler<T> {
    fn from(v: Box<dyn ConditionEvaluatorInner<T>>) -> Self {
        Self::Condition(v)
    }
}

impl<T> From<Box<dyn CellHandlerInner<T>>> for Handler<T> {
    fn from(v: Box<dyn CellHandlerInner<T>>) -> Self {
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
    pub fn new<TInto: Into<Vec<Handler<T>>>>(handlers: TInto) -> Self {
        Self {
            handlers: handlers.into(),
        }
    }

    pub fn run(&self, program: &Cell, input: &T) -> T {
        self.visit(program, input)
    }

    fn visit(&self, cell: &Cell, input: &T) -> T {
        info!("Visiting cell: {cell:?}");
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
            Cell::Custom(CustomCell { id, body }) => {
                let custom_handler = self.select_handler(id);

                info!("Running cell with custom handler: {custom_handler:?}");

                custom_handler.evaluate(input, body, self)
            }
            Cell::NoOp => input.clone(),
        }
    }

    fn select_handler(&self, id: &Id) -> &dyn CellHandlerInner<T> {
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
    use super::{CellHandlerInner, CellVisitor, ConditionEvaluator};
    use crate::{
        visitor::Handler, Cell, CellHandler, CellHandlerConfig, Condition, CustomCell, Id, IfCell,
        SequenceCell,
    };
    use serde::{Deserialize, Serialize};

    #[derive(Debug, Clone)]
    struct MyState(usize);

    #[derive(Debug, Clone)]
    struct Incrementor;

    #[derive(Serialize, Deserialize)]
    struct IncrementorConfig {
        increment_by: usize,
    }

    impl IncrementorConfig {
        const fn new(increment_by: usize) -> Self {
            Self { increment_by }
        }
    }

    impl CellHandlerConfig for IncrementorConfig {
        fn id() -> Id {
            Id::new("incrementor")
        }
    }

    impl CellHandler<MyState> for Incrementor {
        type Config = IncrementorConfig;

        fn id(&self) -> Id {
            Self::Config::id()
        }

        fn evaluate(
            &self,
            item: &MyState,
            config: &IncrementorConfig,
            _visitor: &CellVisitor<MyState>,
        ) -> MyState {
            MyState(item.0 + config.increment_by)
        }
    }

    #[derive(Serialize, Deserialize)]
    struct GreaterThanCondition(usize);

    impl GreaterThanCondition {
        fn id() -> Id {
            Id::new("greater-than-condition")
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
        let _unused: Box<dyn CellHandlerInner<usize>> = todo!("checking object safety");
    }

    #[test]
    fn test_simple_program_1() {
        let program = SequenceCell::new(vec![CustomCell::new(IncrementorConfig::new(1)).into()]);

        let visitor = CellVisitor::new(vec![Handler::Cell(Box::new(Incrementor))]);

        let output = visitor.run(&program.into(), &MyState(5));

        assert_eq!(6, output.0);
    }

    #[test]
    fn test_simple_program_2() {
        let program = SequenceCell::new(vec![
            CustomCell::new(IncrementorConfig::new(1)).into(),
            CustomCell::new(IncrementorConfig::new(2)).into(),
            CustomCell::new(IncrementorConfig::new(3)).into(),
        ]);

        let visitor = CellVisitor::new(vec![Handler::Cell(Box::new(Incrementor))]);

        let output = visitor.run(&program.into(), &MyState(5));

        assert_eq!(11, output.0);
    }

    #[test]
    fn test_simple_program_3() {
        let program = SequenceCell::new(vec![
            CustomCell::new(IncrementorConfig::new(1)).into(),
            CustomCell::new(IncrementorConfig::new(1)).into(),
            Cell::If(IfCell::new(
                Condition::new(GreaterThanCondition::id(), GreaterThanCondition(7)),
                Box::new(CustomCell::new(IncrementorConfig::new(1)).into()),
                Box::new(Cell::NoOp),
            )),
            CustomCell::new(IncrementorConfig::new(1)).into(),
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
            CustomCell::new(IncrementorConfig::new(1)).into(),
            CustomCell::new(IncrementorConfig::new(1)).into(),
            CustomCell::new(IncrementorConfig::new(1)).into(),
            Cell::If(IfCell::new(
                Condition::new(GreaterThanCondition::id(), GreaterThanCondition(7)),
                Box::new(Cell::NoOp),
                Box::new(CustomCell::new(IncrementorConfig::new(1)).into()),
            )),
            CustomCell::new(IncrementorConfig::new(1)).into(),
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
        let if_cell = Cell::If(IfCell::new(
            Condition::new(GreaterThanCondition::id(), GreaterThanCondition(7)),
            Box::new(CustomCell::new(IncrementorConfig::new(2)).into()),
            Box::new(Cell::NoOp),
        ));

        let cell = Cell::Sequence(SequenceCell::new(vec![if_cell]));

        let serialized = serde_json::to_string(&cell).unwrap();

        assert_eq!(
            "{\"Sequence\":{\"sequence\":[{\"If\":{\"condition\":{\"id\":\"greater-than-condition\",\"body\":7},\"on_true\":{\"Custom\":{\"id\":\"incrementor\",\"body\":{\"increment_by\":2}}},\"on_false\":\"NoOp\"}}]}}",
            &serialized);
    }

    #[test]
    fn test_deserialize() {
        let json =
            "{\"Sequence\":{\"sequence\":[{\"If\":{\"condition\":{\"id\":\"greater-than-condition\",\"body\":7},\"on_true\":{\"Custom\":{\"id\":\"incrementor\",\"body\":{\"increment_by\":2}}},\"on_false\":\"NoOp\"}}]}}";

        let deserialized: Cell = serde_json::from_str(json).unwrap();

        let visitor = CellVisitor::new(vec![
            Handler::Cell(Box::new(Incrementor)),
            Handler::Condition(Box::new(GreaterThanConditionEvaluator)),
        ]);

        let output = visitor.run(&deserialized, &MyState(8));
        assert_eq!(10, output.0);

        let output = visitor.run(&deserialized, &MyState(7));
        assert_eq!(7, output.0);
    }
}
