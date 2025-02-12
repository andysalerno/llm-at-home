use crate::{CellVisitor, Handler, Id, Json};
use serde::{Deserialize, Serialize};

/// A trait representing a handler for a type of cell.
/// It maps to the cell type by id.
pub trait CellHandlerInner<T>: std::fmt::Debug {
    fn cell_type_id(&self) -> Id;
    fn instance_id(&self) -> Id;
    fn evaluate(&self, item: &T, cell_config: &Json, visitor: &CellVisitor<T>) -> T;
}

/// A Handler is the logic portion of a Cell.
///
/// It takes &T as input, transforms it, and returns the T output.
/// Since a single Cell may itself cotnain multiple Cells, running "a cell"
/// may actually imply running many.
/// The given `CellVisitor` is responsible for providing all the handlers available
/// to run any other cells in this way.
pub trait CellHandler<T>: std::fmt::Debug {
    type Config: CellHandlerConfig;

    fn cell_type_id(&self) -> Id {
        Self::Config::cell_type_id()
    }

    fn instance_id(&self) -> Id {
        Id::new_empty()
    }

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
    fn evaluate(&self, item: &TItem, condition_body: &Json, visitor: &CellVisitor<TItem>) -> TItem {
        let parsed: T::Config = serde_json::from_value(condition_body.0.clone()).unwrap();

        CellHandler::evaluate(self, item, &parsed, visitor)
    }

    fn cell_type_id(&self) -> Id {
        self.cell_type_id()
    }

    fn instance_id(&self) -> Id {
        self.instance_id()
    }
}

pub trait CellHandlerConfig: Serialize + for<'a> Deserialize<'a> {
    fn cell_type_id() -> Id;
    fn instance_id(&self) -> Id {
        Id::new_empty()
    }
}

pub struct HandlerCollection<T> {
    handlers: Vec<Box<dyn CellHandlerInner<T>>>,
}

impl<T> HandlerCollection<T> {
    #[must_use]
    pub fn new() -> Self {
        Self {
            handlers: Vec::new(),
        }
    }

    pub fn add<H: CellHandler<T> + 'static>(mut self, handler: H) -> Self {
        self.handlers.push(Box::new(handler));
        self
    }

    #[must_use]
    pub fn take_all(self) -> Vec<Box<dyn CellHandlerInner<T>>> {
        self.handlers
    }
}

impl<T> From<HandlerCollection<T>> for Vec<Handler<T>> {
    fn from(collection: HandlerCollection<T>) -> Self {
        collection
            .take_all()
            .into_iter()
            .map(|h| Handler::Cell(h))
            .collect()
    }
}

impl<T> Default for HandlerCollection<T> {
    fn default() -> Self {
        Self::new()
    }
}
