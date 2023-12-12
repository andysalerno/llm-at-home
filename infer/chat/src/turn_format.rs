//! The `TurnFormat` types.
use derive_builder::Builder;

/// The turn format.
#[allow(missing_docs)]
#[derive(Builder, Clone, Debug)]
#[builder(setter(into))]
pub struct TurnFormat {
    #[builder(default)]
    system_prefix: String,

    #[builder(default)]
    user_prefix: String,

    #[builder(default)]
    system_postfix: String,

    #[builder(default)]
    assistant_prefix: String,

    #[builder(default)]
    assistant_postfix: String,

    #[builder(default)]
    user_postfix: String,

    #[builder(default)]
    function_prefix: String,

    #[builder(default)]
    function_postfix: String,

    #[builder(default)]
    eos_token: String,
}

impl TurnFormat {
    /// Gets the system prefix.
    #[must_use]
    pub fn system_prefix(&self) -> &str {
        self.system_prefix.as_ref()
    }

    /// Gets the user prefix.
    #[must_use]
    pub fn user_prefix(&self) -> &str {
        self.user_prefix.as_ref()
    }

    /// Gets the system postfix.
    #[must_use]
    pub fn system_postfix(&self) -> &str {
        self.system_postfix.as_ref()
    }

    /// Gets the user postfix.
    #[must_use]
    pub fn user_postfix(&self) -> &str {
        self.user_postfix.as_ref()
    }

    /// Gets the eos token.
    #[must_use]
    pub fn eos_token(&self) -> &str {
        self.eos_token.as_ref()
    }

    /// Gets the assistant prefix.
    #[must_use]
    pub fn assistant_prefix(&self) -> &str {
        self.assistant_prefix.as_ref()
    }

    /// Gets the assistant postfix.
    #[must_use]
    pub fn assistant_postfix(&self) -> &str {
        self.assistant_postfix.as_ref()
    }

    /// Gets the function prefix.
    #[must_use]
    pub fn function_prefix(&self) -> &str {
        self.function_prefix.as_ref()
    }

    /// Gets the function postfix.
    #[must_use]
    pub fn function_postfix(&self) -> &str {
        self.function_postfix.as_ref()
    }
}
