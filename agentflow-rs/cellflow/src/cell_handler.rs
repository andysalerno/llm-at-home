use crate::{CellVisitor, Handler, Id, Json};
use serde::Deserialize;

/// A trait representing a handler for a type of cell.
/// It maps to the cell type by id.
pub trait CellHandlerInner<T> {
    fn name(&self) -> Id;
    fn evaluate(&self, item: &T, cell_config: &Json, visitor: &CellVisitor<T>) -> T;
}

/// A Handler is the logic portion of a Cell.
///
/// It takes &T as input, transforms it, and returns the T output.
/// Since a single Cell may itself cotnain multiple Cells, running "a cell"
/// may actually imply running many.
/// The given `CellVisitor` is responsible for providing all the handlers available
/// to run any other cells in this way.
pub trait CellHandler<T> {
    type Config: for<'a> Deserialize<'a>;
    fn name(&self) -> Id;
    fn evaluate(&self, item: &T, cell_config: &Self::Config, visitor: &CellVisitor<T>) -> T;
    fn into_handler(self) -> Handler<T>
    where
        Self: Sized + 'static,
    {
        Handler::Cell(Box::new(self))
    }
}

impl<T, TItem> CellHandlerInner<TItem> for T
where
    T: CellHandler<TItem>,
{
    fn name(&self) -> Id {
        CellHandler::name(self)
    }

    fn evaluate(&self, item: &TItem, condition_body: &Json, visitor: &CellVisitor<TItem>) -> TItem {
        let parsed: T::Config = serde_json::from_value(condition_body.0.clone()).unwrap();

        CellHandler::evaluate(self, item, &parsed, visitor)
    }
}
