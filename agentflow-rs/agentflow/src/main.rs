use cellflow::{CellVisitor, CustomCell, Handler, SequenceCell};

mod cells;

fn main() {
    // let program = SequenceCell::new(vec![
    //     CustomCell::new(Incrementor::id(), Json::from(&"{}")).into()
    // ]);

    // let visitor = CellVisitor::new(vec![Handler::Cell(Box::new(Incrementor))]);

    // let output = visitor.run(&program.into(), &MyState(5));
}
