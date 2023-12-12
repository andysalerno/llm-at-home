//! A runner for executing a chat loop with function calling.
#![allow(clippy::multiple_crate_versions)]
mod api;
mod chat_loop;
mod formats;
mod openai_server;

use crate::openai_server::OpenAIServer;
use chat::ChatTemplate;
use env_logger::Env;
use functions::{NoOp, WebSearch};
use libinfer::{function::Function, llm_client::LLMClient};
use log::{debug, info};
use model_client::TgiClient;
use std::io::Write;

#[tokio::main]
async fn main() {
    {
        let log_level = "info";
        let env = Env::default().filter_or(
            "RUST_LOG",
            format!("runner={log_level},model_client={log_level},chat={log_level},functions={log_level},libinfer={log_level}"),
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

fn detect_chat_template(model_name: &str) -> ChatTemplate {
    if model_name.contains("CausalLM") {
        info!("Detected turn format: causal_lm");
        todo!()
    } else if model_name.to_lowercase().contains("llama-2") {
        info!("Detected turn format: Llama-2");
        formats::llama2_chat()
    } else if model_name.to_lowercase().contains("mistral-7b-instruct") {
        info!("Detected turn format: mistral-instruct");
        formats::mistral_instruct()
    } else if model_name.contains("zephyr") {
        info!("Detected turn format: zephyr");
        formats::zephyr()
    } else if model_name.contains("dolphin") {
        info!("Detected turn format: dolphin");
        formats::dolphin()
    } else if model_name.contains("openhermes") || model_name.contains("skywork") {
        info!("Detected turn format: chatml");
        todo!()
    } else if model_name.contains("agentlm") {
        info!("Detected turn format: agentlm");
        formats::llama2_chat()
    } else if model_name.contains("openchat_3.5") {
        info!("Detected turn format: openchat");
        formats::openchat()
    } else if model_name.to_lowercase().contains("starling") {
        info!("Detected turn format: starling (openchat)");
        formats::starling()
    } else if model_name.to_lowercase().contains("synthia") {
        info!("Detected turn format: synthia");
        formats::synthia()
    } else if model_name.to_lowercase().contains("hermes") {
        info!("Detected turn format: hermes");
        formats::hermes()
    } else if model_name.to_lowercase().contains("airoboros") {
        info!("Detected turn format: airoboros");
        formats::llama2_chat()
    } else if model_name.to_lowercase().contains("neural") {
        info!("Detected turn format: neural");
        formats::neural()
    } else if model_name.to_lowercase().contains("grendel") {
        info!("Detected turn format: grendel");
        formats::grendel()
    } else if model_name.to_lowercase().contains("mistrallite") {
        info!("Detected turn format: mistrallite");
        formats::amazon_mistral_lite()
    } else if model_name.to_lowercase().contains("capybara") {
        info!("Detected turn format: capybara");
        formats::yi_capybara_nous()
    } else if model_name.to_lowercase().contains("deepseek") {
        info!("Detected turn format: deepseek");
        formats::deepseek_coder()
    } else if model_name.to_lowercase().contains("slimorcaboros") {
        info!("Detected turn format: slimorcaboros");
        formats::mistral_slimorcaboros()
    } else if model_name.to_lowercase().contains("orca-2") {
        info!("Detected turn format: orca-2");
        formats::orca2()
    } else if model_name.contains("Xwin-LM") {
        info!("Detected turn format: xwin");
        todo!()
    } else {
        panic!("Unable to detect chat turn format for model id: {model_name}");
    }
}

fn get_user_input() -> String {
    print!("input: ");
    std::io::stdout().flush().unwrap();
    let mut buffer = String::new();
    std::io::stdin()
        .read_line(&mut buffer)
        .expect("could not get input from user");

    buffer.trim().into()
}
