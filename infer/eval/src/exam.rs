use std::{fs::File, io::Write};

use chat::history::History;
use derive_builder::Builder;
use libinfer::chat_client::ChatClient;
use log::info;
use serde::{Deserialize, Serialize};

#[derive(Debug, Builder)]
pub(crate) struct Exam {
    problems: Vec<Problem>,
}

#[derive(Debug, Clone, Serialize, Deserialize, Builder)]
pub(crate) struct Answer {
    problem_title: String,
    text: String,
}

#[derive(Debug, Clone, Builder)]
pub(crate) struct Problem {
    /// A human-friendly title for the problem.
    #[builder(setter(into))]
    title: String,

    /// A chat history. Possible single-turn, possible multi-turn.
    /// We provide this history to the LLM we are evaluating,
    /// and capture its response. This response will be graded against the known answer.
    history: History,

    /// The accepted answer. We will ask an LLM to compare the given answer to this expected answer, and grade it as pass/fail.
    answer: Answer,
}

impl Exam {
    fn new(problems: Vec<Problem>) -> Self {
        Self { problems }
    }

    pub async fn run(&self, chat_client: &ChatClient) -> Vec<Answer> {
        info!("Starting example with {} problems", self.problems.len());

        let mut answers = Vec::new();

        for problem in &self.problems {
            info!("Getting model response for problem: {}", &problem.title);

            let history = &problem.history;

            let response = chat_client.get_assistant_response(history).await;

            info!("Got model response for problem: {}", &problem.title);
            answers.push(Answer {
                problem_title: problem.title.clone(),
                text: response.content().to_owned(),
            });
        }

        answers
    }

    pub fn persist_answers_to_disk(answers: &[Answer]) {
        let dir = "results";
        std::fs::create_dir_all(dir).unwrap();

        for answer in answers {
            let json = serde_json::to_string_pretty(answer).unwrap();
            let path = format!("{}/{}.txt", dir, answer.problem_title);
            let mut file = File::create(&path).unwrap();

            info!("Writing: {path}");
            file.write_all(json.as_bytes()).unwrap();
        }
    }
}
