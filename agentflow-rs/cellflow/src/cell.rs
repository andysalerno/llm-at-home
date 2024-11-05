use serde::{Deserialize, Serialize};

/// A serializable and deserializable representation of some operation.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum Cell {
    If(IfCell),
    While(WhileCell),
    Sequence(SequenceCell),
    Custom(Id),
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

#[derive(Debug, Clone, Serialize, Deserialize, Eq, PartialEq, PartialOrd, Ord)]
pub struct Id(String);

impl Id {
    #[must_use]
    pub const fn new(value: String) -> Self {
        Self(value)
    }
}

#[derive(Debug, Clone, Serialize, Deserialize, Eq, PartialEq)]
pub struct Json(serde_json::Value);

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

#[cfg(test)]
mod tests {
    use super::{Cell, SequenceCell};

    #[test]
    fn test_simple_sequence() {
        let _cell = Cell::Sequence(SequenceCell::new(Vec::new()));
    }
}
