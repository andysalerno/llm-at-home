//! A runner for executing a chat loop with function calling.
#![allow(clippy::multiple_crate_versions)]
mod api;
mod function_selection;
mod openai_server;

use crate::openai_server::OpenAIServer;
use env_logger::Env;
use functions::{NoOp, WebSearch};
use libinfer::{function::Function, llm_client::LLMClient};
use log::{debug, info};
use tgi_client::TgiClient;

#[tokio::main]
async fn main() {
    {
        let log_level = "info";
        let env = Env::default().filter_or(
            "RUST_LOG",
            format!("runner={log_level},model_client={log_level},chat={log_level},functions={log_level},libinfer={log_level},axum={log_level}"),
        );
        env_logger::init_from_env(env);
        debug!("Starting up.");
    }

    let endpoint = std::env::args()
        .nth(1)
        .expect("Expected a single arg for the endpoint");

    info!("Starting with endpoint: {endpoint}");

    let client = TgiClient::new(endpoint);

    let info = client.get_info().await;
    info!("Server is hosting model: {}", info.model_id());

    let functions: Vec<Box<dyn Function + Send + Sync>> = vec![Box::new(WebSearch), Box::new(NoOp)];

    OpenAIServer::serve(client, functions).await;
    // chat_loop(client, functions).await;
}
