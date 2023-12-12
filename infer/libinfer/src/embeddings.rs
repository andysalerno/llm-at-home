use serde::{Deserialize, Serialize};

/// A request to get embeddings for the given inputs.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct EmbeddingsRequest {
    /// The inputs for which to generate embeddings.
    pub input: Vec<String>,
}

impl EmbeddingsRequest {
    /// Create a new request.
    #[must_use]
    pub fn new(input: Vec<String>) -> Self {
        Self { input }
    }
}

/// A response with embeddings.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct EmbeddingsResponse {
    /// The object value.
    pub object: String,

    /// The embedding data.
    pub data: Vec<Embedding>,

    /// The name of the model that generated the embedding.
    pub model: String,
}

impl EmbeddingsResponse {
    /// Consume self and take the embeddings.
    #[must_use]
    pub fn take_embeddings(self) -> Vec<Embedding> {
        self.data
    }
}

/// An embedding.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Embedding {
    object: String,
    embedding: Vec<f32>,
    index: usize,
}

impl Embedding {
    /// Gets the index.
    #[must_use]
    pub fn index(&self) -> usize {
        self.index
    }

    /// Gets the embeddings.
    #[must_use]
    pub fn embedding(&self) -> &[f32] {
        self.embedding.as_ref()
    }
}
