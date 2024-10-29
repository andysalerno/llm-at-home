use crate::cell::Cell;

pub fn run<T>(root: &impl Cell<T>, input: &T) -> T {
    root.run(input)
}
