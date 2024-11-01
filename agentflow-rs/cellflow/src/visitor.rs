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

enum Handler<T> {
    Cell(Box<dyn CellHandler<T>>),
    Condition(Box<dyn ConditionEvaluator<T>>),
}

/// A visitor or interpreter that can visit a cell
/// and perform the given handler for it.
pub struct CellVisitor<T> {
    handlers: Vec<Handler<T>>,
}

impl<T> CellVisitor<T> {
    pub fn visit(&self, cell: &Cell, input: &T) {
        match cell {
            Cell::If(if_cell) => {
                let condition_handler = self.select_condition(if_cell.condition().id());

                if condition_handler.evaluate(input) {
                    self.visit(if_cell.on_true(), input);
                } else {
                    self.visit(if_cell.on_false(), input);
                }
            }
            Cell::While(while_cell) => {
                let condition_handler = self.select_condition(while_cell.condition().id());

                while condition_handler.evaluate(input) {
                    self.visit(while_cell.body(), input);
                }
            }
            Cell::Sequence(sequence_cell) => todo!(),
            Cell::Custom(id) => todo!(),
        }
    }

    pub fn run(&self, program: &Cell, input: &T) {}

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
    use super::CellHandler;

    #[test]
    #[should_panic(expected = "checking object safety")]
    fn verify_object_safety() {
        let _unused: Box<dyn CellHandler<usize>> = todo!("checking object safety");
    }
}
