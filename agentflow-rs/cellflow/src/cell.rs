pub trait Cell<T> {
    fn run(&self, input: &T) -> T;
}

pub struct NoOpCell<T> {
    phantom: std::marker::PhantomData<T>,
}

impl<T> NoOpCell<T> {
    #[must_use]
    pub const fn new() -> Self {
        Self {
            phantom: std::marker::PhantomData,
        }
    }
}

impl<T> Default for NoOpCell<T> {
    fn default() -> Self {
        Self::new()
    }
}

impl<T: Clone> Cell<T> for NoOpCell<T> {
    fn run(&self, input: &T) -> T {
        input.clone()
    }
}
