//! This crate is used to represent a chat history.
#![allow(clippy::bool_to_int_with_if)]
#![allow(clippy::multiple_crate_versions)]
pub mod history;
mod jinjarender;
pub mod turn_format;

pub use jinjarender::{ChatTemplate, FunctionStyle, Renderer};

/// An enum representing the role of a message.
#[derive(PartialEq, Debug, Clone, Copy)]
pub enum Role {
    /// The user role.
    User,

    /// The system role.
    System,

    /// The function role.
    Function,

    /// The assistant role.
    Assistant,
}

impl Role {
    /// Gets the role as a str.
    #[must_use]
    pub fn as_str(&self) -> &str {
        match self {
            Role::User => "user",
            Role::System => "system",
            Role::Function => "function",
            Role::Assistant => "assistant",
        }
    }

    /// Parse the enum value from a str.
    /// # Panics
    /// Panics if the str is not recognized as a role.
    #[must_use]
    pub fn parse(text: &str) -> Self {
        match text {
            "user" => Role::User,
            "system" => Role::System,
            "function" => Role::Function,
            "assistant" => Role::Assistant,
            _ => panic!("Unknown role: {text}"),
        }
    }
}

/// A type to represent the message.
#[derive(Clone, Debug)]
pub struct Message {
    role: Role,
    content: String,
}

impl Message {
    /// Create a new message for the given role and content.
    pub fn new(role: Role, content: impl AsRef<str>) -> Self {
        let content = content.as_ref();

        let content = content.trim().into();

        Self { role, content }
    }

    /// Gets the role.
    #[must_use]
    pub fn role(&self) -> &Role {
        &self.role
    }

    /// Gets the content.
    #[must_use]
    pub fn content(&self) -> &str {
        self.content.as_ref()
    }
}
