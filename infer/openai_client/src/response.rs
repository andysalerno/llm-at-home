use serde::{Deserialize, Serialize};

// {
//     "id": "cmpl-6ea98e1b9bdd4f3183dd9763528b9c67",
//     "object": "text_completion",
//     "created": 12584,
//     "model": "model",
//     "choices": [
//         {
//             "index": 0,
//             "text": "，",
//             "logprobs": null,
//             "finish_reason": null
//         }
//     ],
//     "usage": null
// }

#[derive(Debug, Serialize, Deserialize)]
pub(crate) struct Delta {
    content: Option<String>,
}

#[derive(Debug, Serialize, Deserialize)]
pub(crate) struct Choice {
    index: usize,
    text: String,
    finish_reason: Option<String>,
}

impl Choice {
    pub(crate) fn text(&self) -> &str {
        &self.text
    }
}

#[derive(Debug, Serialize, Deserialize)]
pub(crate) struct Response {
    id: String,
    object: String,
    choices: Vec<Choice>,
    created: usize,
    model: String,
}

impl Response {
    pub(crate) fn choices(&self) -> &[Choice] {
        &self.choices
    }
}
