use std::marker::PhantomData;

/// A serializable and deserializable representation of some operation.
pub enum Cell<T> {
    If(IfCell<T>),
    While(WhileCell<T>),
    Sequence(SequenceCell<T>),
    Custom(CustomCell<T>),
    NoOp,
}

impl<T> From<SequenceCell<T>> for Cell<T> {
    fn from(v: SequenceCell<T>) -> Self {
        Self::Sequence(v)
    }
}

impl<T> From<WhileCell<T>> for Cell<T> {
    fn from(v: WhileCell<T>) -> Self {
        Self::While(v)
    }
}

impl<T> From<IfCell<T>> for Cell<T> {
    fn from(v: IfCell<T>) -> Self {
        Self::If(v)
    }
}

impl<T> From<CustomCell<T>> for Cell<T> {
    fn from(v: CustomCell<T>) -> Self {
        Self::Custom(v)
    }
}

pub struct Id(String);

impl Id {
    #[must_use]
    pub const fn new(value: String) -> Self {
        Self(value)
    }
}

// T is the config, "data"
// There's a CellHandler with an associated type, "logic"
// Visitor looks at the cell, finds the right CellHandler, and executes
pub struct CustomCell<T> {
    pub(crate) id: Id,
    pub behavior: Box<dyn FnOnce(&T) -> T>,
}

pub struct Condition<T> {
    id: Id,
    body: String,
    phantom_data: PhantomData<T>,
}

pub struct IfCell<T> {
    condition: Condition<T>,
    on_true: Box<Cell<T>>,
    on_false: Box<Cell<T>>,
}

impl<T> IfCell<T> {
    #[must_use]
    pub const fn new(
        condition: Condition<T>,
        on_true: Box<Cell<T>>,
        on_false: Box<Cell<T>>,
    ) -> Self {
        Self {
            condition,
            on_true,
            on_false,
        }
    }

    #[must_use]
    pub const fn condition(&self) -> &Condition<T> {
        &self.condition
    }

    #[must_use]
    pub const fn on_true(&self) -> &Cell<T> {
        &self.on_true
    }

    #[must_use]
    pub const fn on_false(&self) -> &Cell<T> {
        &self.on_false
    }
}

pub struct WhileCell<T> {
    condition: Condition<T>,
    body: Box<Cell<T>>,
}

impl<T> WhileCell<T> {
    #[must_use]
    pub const fn condition(&self) -> &Condition<T> {
        &self.condition
    }

    #[must_use]
    pub const fn body(&self) -> &Cell<T> {
        &self.body
    }
}

pub struct SequenceCell<T> {
    sequence: Vec<Cell<T>>,
}

impl<T> SequenceCell<T> {
    #[must_use]
    pub const fn new(sequence: Vec<Cell<T>>) -> Self {
        Self { sequence }
    }

    #[must_use]
    pub fn sequence(&self) -> &[Cell<T>] {
        &self.sequence
    }
}

#[cfg(test)]
mod tests {
    use super::{Cell, SequenceCell};

    #[test]
    fn test_simple_sequence() {
        let _cell = Cell::Sequence(SequenceCell::<String>::new(Vec::new()));
    }
}
