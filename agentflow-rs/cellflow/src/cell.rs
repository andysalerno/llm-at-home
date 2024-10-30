pub enum Cell {
    If(IfCell),
    While(WhileCell),
    Sequence(SequenceCell),
    Custom,
}

pub struct Condition;

pub struct IfCell {
    on_true: Box<Cell>,
    on_false: Box<Cell>,
}

pub struct WhileCell;

pub struct SequenceCell(Vec<Cell>);
