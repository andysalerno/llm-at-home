use crate::cell::Cell;

pub trait CellHandler<T> {
    fn id(&self) -> String;
    fn handle(&self, item: &T) -> T;
}

pub struct CellVisitor<T> {
    handlers: Vec<Box<dyn CellHandler<T>>>,
}

impl<T> CellVisitor<T> {
    pub fn visit(cell: &Cell, input: &T) {
        match cell {
            Cell::If(if_cell) => todo!(),
            Cell::While(while_cell) => todo!(),
            Cell::Sequence(sequence_cell) => todo!(),
            Cell::Custom => todo!(),
        }
    }

    fn select_handler(&self, input: &T) -> &dyn CellHandler<T> {
        self.handlers.first().unwrap().as_ref()
    }
}

#[cfg(test)]
mod tests {
    use super::CellHandler;

    #[test]
    #[should_panic]
    fn verify_object_safety() {
        let _: Box<dyn CellHandler<usize>> = todo!();
    }
}
