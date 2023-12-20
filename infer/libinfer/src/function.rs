use async_trait::async_trait;

use crate::chat_client::ChatClient;

/// A function that can be invoked by the LLM.
#[async_trait]
pub trait Function {
    /// The name of the function.
    fn name(&self) -> &str;

    /// A description for the model.
    fn description_for_model(&self) -> &str;

    /// Get the output of the function.
    async fn get_output(&self, input: &str, client: &ChatClient) -> String;
}
