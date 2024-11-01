use serde::{Deserialize, Serialize};

/// A serializable and deserializable representation of some operation.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum Cell {
    If(IfCell),
    While(WhileCell),
    Sequence(SequenceCell),
    Custom(Id),
}

#[derive(Debug, Clone, Serialize, Deserialize, Eq, PartialEq, PartialOrd, Ord)]
pub struct Id(String);

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Condition {
    id: Id,
}

impl Condition {
    pub fn id(&self) -> &Id {
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
    pub fn condition(&self) -> &Condition {
        &self.condition
    }

    pub fn on_true(&self) -> &Cell {
        &self.on_true
    }

    pub fn on_false(&self) -> &Cell {
        &self.on_false
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct WhileCell {
    condition: Condition,
    body: Box<Cell>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SequenceCell(Vec<Cell>);

#[cfg(test)]
mod tests {
    use super::{Cell, SequenceCell};

    #[test]
    fn test_simple_sequence() {
        let _cell = Cell::Sequence(SequenceCell(Vec::new()));
    }
}
