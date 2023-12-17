//! Evaluator
#![allow(clippy::multiple_crate_versions)]

mod exam;
mod problems;
mod proctor;

use std::{fs::File, io::Write};

use crate::exam::{Exam, ExamBuilder};
use chat_formats::detect_chat_template;
use env_logger::Env;
use exam::Answer;
use libinfer::{chat_client::ChatClient, llm_client::LLMClient};
use log::{debug, info};
use model_client::TgiClient;

#[tokio::main]
async fn main() {
    {
        let log_level = "info";
        let env = Env::default().filter_or("RUST_LOG", log_level);
        env_logger::init_from_env(env);
        debug!("Starting up.");
    }

    let endpoint = std::env::args()
        .nth(1)
        .expect("Expected a single arg for the endpoint");

    info!("Starting with endpoint: {endpoint}");

    let client = TgiClient::new(endpoint);

    let info = client.get_info().await;
    let chat_template = detect_chat_template(info.model_id());
    let chat_client = ChatClient::new(Box::new(client), chat_template);

    let exam = ExamBuilder::default()
        .problems(vec![
            problems::system_message_compliance_pistachio_easy(),
            problems::system_message_compliance_pistachio_medium(),
            problems::system_message_compliance_pistachio_hard(),
        ])
        .build()
        .unwrap();

    let answers = exam.run(&chat_client).await;

    persist_answers_to_disk(info.model_id(), &answers);
}

fn persist_answers_to_disk(model_name: &str, answers: &[Answer]) {
    let dir = format!("results/{model_name}");
    std::fs::create_dir_all(&dir).unwrap();

    for answer in answers {
        let json = serde_json::to_string_pretty(answer).unwrap();
        let path = format!("{}/{}.txt", dir, answer.problem_title());
        let mut file = File::create(&path).unwrap();

        info!("Writing: {path}");
        file.write_all(json.as_bytes()).unwrap();
    }
}
