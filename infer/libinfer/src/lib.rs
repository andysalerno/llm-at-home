//! The infer module.
#![allow(
    clippy::missing_panics_doc,
    clippy::module_name_repetitions,
    clippy::multiple_crate_versions
)]

/// Chat client module.
pub mod chat_client;

/// Document module.
pub mod document;

/// Embeddings module.
pub mod embeddings;

/// Function module.
pub mod function;

/// Generate request module.
pub mod generate_request;

/// Info module.
pub mod info;

/// LLM client module.
pub mod llm_client;

use log::info;

/// Read a prompt with the given name from the prompts directory.
#[must_use]
pub fn read_prompt(name: &str) -> String {
    let prompt_path = format!("prompts/{name}");

    info!("Reading prompt from: {prompt_path}");

    std::fs::read_to_string(prompt_path).expect("Unable to read prompt file")
}
