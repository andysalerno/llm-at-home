use chat::history::History;
use derive_builder::Builder;
use libinfer::chat_client::ChatClient;
use log::info;
use serde::{Deserialize, Serialize};

#[derive(Debug, Builder)]
pub(crate) struct Exam {
    problems: Vec<Problem>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub(crate) struct Answer {
    problem_title: String,
    text: String,
    expected_answer: String,
}

impl Answer {
    pub(crate) fn problem_title(&self) -> &str {
        self.problem_title.as_ref()
    }

    pub(crate) fn text(&self) -> &str {
        self.text.as_ref()
    }
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
    #[builder(setter(into))]
    expected_answer: String,
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
                expected_answer: problem.expected_answer.clone(),
            });
        }

        answers
    }
}
