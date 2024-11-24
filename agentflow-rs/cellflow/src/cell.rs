use std::error::Error;

use serde::{Deserialize, Serialize};

use crate::CellHandlerConfig;

/// A serializable and deserializable representation of some operation.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum Cell {
    If(IfCell),
    While(WhileCell),
    Sequence(SequenceCell),
    Custom(CustomCell),
    NoOp,
}

impl From<SequenceCell> for Cell {
    fn from(v: SequenceCell) -> Self {
        Self::Sequence(v)
    }
}

impl From<WhileCell> for Cell {
    fn from(v: WhileCell) -> Self {
        Self::While(v)
    }
}

impl From<IfCell> for Cell {
    fn from(v: IfCell) -> Self {
        Self::If(v)
    }
}

impl From<CustomCell> for Cell {
    fn from(v: CustomCell) -> Self {
        Self::Custom(v)
    }
}

#[derive(Debug, Clone, Serialize, Deserialize, Eq, PartialEq, PartialOrd, Ord)]
pub struct Id(String);

impl Id {
    #[must_use]
    pub const fn new_const(value: String) -> Self {
        Self(value)
    }

    pub fn new(value: impl Into<String>) -> Self {
        Self(value.into())
    }
}

#[derive(Debug, Clone, Serialize, Deserialize, Eq, PartialEq)]
pub struct Json(pub(crate) serde_json::Value);

impl Json {
    pub fn to<T: for<'a> Deserialize<'a>>(&self) -> Result<T, Box<dyn Error>> {
        match serde_json::from_value(self.0.clone()) {
            Ok(v) => Ok(v),
            Err(e) => Err(Box::new(e)),
        }
    }

    pub fn from<T: Serialize>(input: &T) -> Self {
        Self(serde_json::to_value(input).unwrap())
    }
}

#[derive(Debug, Clone, Serialize, Deserialize, Eq, PartialEq)]
pub struct CustomCell {
    pub(crate) id: Id,
    pub(crate) body: Json,
}

impl CustomCell {
    pub fn new<T: CellHandlerConfig>(config: T) -> Self {
        Self {
            id: T::id(),
            body: Json(serde_json::to_value(config).expect("could not serialize the body")),
        }
    }

    #[must_use]
    pub const fn id(&self) -> &Id {
        &self.id
    }

    #[must_use]
    pub const fn body(&self) -> &Json {
        &self.body
    }
}

impl<T: CellHandlerConfig> From<T> for CustomCell {
    fn from(config: T) -> Self {
        CustomCell::new(config)
    }
}

#[derive(Debug, Clone, Serialize, Deserialize, Eq, PartialEq)]
pub struct Condition {
    id: Id,
    body: Json,
}

impl Condition {
    #[must_use]
    pub fn new(id: Id, body: impl Serialize) -> Self {
        Self {
            id,
            body: Json(serde_json::to_value(body).expect("could not serialize the body")),
        }
    }

    #[must_use]
    pub const fn id(&self) -> &Id {
        &self.id
    }

    #[must_use]
    pub const fn body(&self) -> &Json {
        &self.body
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct IfCell {
    condition: Condition,
    on_true: Box<Cell>,
    on_false: Box<Cell>,
}

impl IfCell {
    #[must_use]
    pub const fn new(condition: Condition, on_true: Box<Cell>, on_false: Box<Cell>) -> Self {
        Self {
            condition,
            on_true,
            on_false,
        }
    }

    #[must_use]
    pub const fn condition(&self) -> &Condition {
        &self.condition
    }

    #[must_use]
    pub const fn on_true(&self) -> &Cell {
        &self.on_true
    }

    #[must_use]
    pub const fn on_false(&self) -> &Cell {
        &self.on_false
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct WhileCell {
    condition: Condition,
    body: Box<Cell>,
}

impl WhileCell {
    #[must_use]
    pub const fn condition(&self) -> &Condition {
        &self.condition
    }

    #[must_use]
    pub const fn body(&self) -> &Cell {
        &self.body
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SequenceCell {
    sequence: Vec<Cell>,
}

impl SequenceCell {
    #[must_use]
    pub const fn new(sequence: Vec<Cell>) -> Self {
        Self { sequence }
    }

    #[must_use]
    pub fn sequence(&self) -> &[Cell] {
        &self.sequence
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SequenceCellBuilder {
    sequence: Vec<Cell>,
}

impl SequenceCellBuilder {
    pub fn new() -> Self {
        Self {
            sequence: Vec::new(),
        }
    }

    pub fn add<TCell: Into<Cell>>(mut self, cell: TCell) -> Self {
        self.sequence.push(cell.into());

        self
    }

    pub fn build(self) -> SequenceCell {
        SequenceCell::new(self.sequence)
    }
}

#[cfg(test)]
mod tests {
    use super::{Cell, SequenceCell};

    #[test]
    fn test_simple_sequence() {
        let _cell = Cell::Sequence(SequenceCell::new(Vec::new()));
    }
}
