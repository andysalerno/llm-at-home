use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize)]
struct Data {
    id: String,
}

/// Results for a request to the info endpoint.
#[derive(Debug, Serialize, Deserialize)]
pub(crate) struct Info {
    data: Vec<Data>,
}

#[allow(missing_docs, unused)]
impl Info {
    #[must_use]
    pub fn model_id(&self) -> &str {
        &self.data.first().expect("Expected at least one model").id
    }
}

impl From<Info> for libinfer::info::Info {
    fn from(mut value: Info) -> Self {
        let data = value.data.pop().unwrap();
        libinfer::info::Info::new(data.id)
    }
}
