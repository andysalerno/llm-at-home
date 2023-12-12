//! The history types.
use derive_builder::Builder;

use crate::{turn_format::TurnFormat, Message, Role};

/// An ordered history of messages.
#[derive(Clone, Debug)]
pub struct History {
    messages: Vec<Message>,
}

impl Default for History {
    fn default() -> Self {
        Self::new()
    }
}

impl History {
    /// Create a new, empty history.
    #[must_use]
    pub fn new() -> Self {
        Self {
            messages: Vec::new(),
        }
    }

    /// Add a message to the end of history.
    pub fn add(&mut self, message: Message) {
        self.messages.push(message);
    }

    /// Get messages.
    #[must_use]
    pub fn messages(&self) -> &[Message] {
        self.messages.as_ref()
    }

    /// Update the first system prompt in history to the provided text.
    pub fn set_initial_system_message(&mut self, text: String) {
        if let Some(message) = self.messages.get_mut(0) {
            if message.role() == &Role::System {
                *message = Message::new(Role::System, text);
            }
        } else {
            self.messages.insert(0, Message::new(Role::System, text));
        }
    }
}

/// The settings used to configure a rendering of history into text.
#[allow(missing_docs)]
#[derive(Builder)]
pub struct RenderHistorySettings<'a> {
    #[builder(setter(strip_option), default)]
    formatter: Option<&'a TurnFormat>,

    #[builder(default = "false")]
    skip_system: bool,

    #[builder(default = "false")]
    nudge_assistant: bool,

    #[builder(default = "false")]
    skip_functions: bool,
}

impl<'a> RenderHistorySettings<'a> {
    /// Gets the formatter.
    #[must_use]
    pub fn formatter(&self) -> Option<&TurnFormat> {
        self.formatter
    }

    /// Gets the `skip_system` flag.
    #[must_use]
    pub fn skip_system(&self) -> bool {
        self.skip_system
    }

    /// Gets the `nudge_assistant` flag.
    #[must_use]
    pub fn nudge_assistant(&self) -> bool {
        self.nudge_assistant
    }

    /// Gets the `skip_functions` flag.
    #[must_use]
    pub fn skip_functions(&self) -> bool {
        self.skip_functions
    }
}
