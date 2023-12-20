use async_trait::async_trait;
use libinfer::{chat_client::ChatClient, function::Function};

/// A no-op function that does nothing.
pub struct NoOp;

#[async_trait]
impl Function for NoOp {
    fn name(&self) -> &str {
        "no_op"
    }

    fn description_for_model(&self) -> &str {
        ""
    }

    async fn get_output(&self, _: &str, _: &ChatClient) -> String {
        String::new()
    }
}
