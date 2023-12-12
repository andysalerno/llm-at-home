use minijinja::Environment;
use serde::{Deserialize, Serialize};

use crate::{
    history::{RenderHistorySettings, RenderHistorySettingsBuilder},
    Role,
};

#[derive(Debug, Serialize, Deserialize)]
struct History {
    messages: Vec<Message>,
    eos_token: String,
    bos_token: String,
    add_generation_prompt: bool,
}

#[allow(clippy::similar_names)]
impl History {
    fn new(
        messages: Vec<Message>,
        bos_token: &str,
        eos_token: &str,
        add_generation_prompt: bool,
    ) -> Self {
        Self {
            messages,
            eos_token: eos_token.to_owned(),
            bos_token: bos_token.to_owned(),
            add_generation_prompt,
        }
    }
}

#[derive(Debug, Serialize, Deserialize)]
struct Message {
    role: String,
    content: String,
}

impl Message {
    fn new(role: &str, content: &str) -> Self {
        Self {
            role: role.to_owned(),
            content: content.to_owned(),
        }
    }

    pub fn role(&self) -> &str {
        &self.role
    }

    pub fn content(&self) -> &str {
        &self.content
    }

    fn set_content(&mut self, content: String) {
        self.content = content;
    }
}

impl From<&crate::Message> for Message {
    fn from(value: &crate::Message) -> Self {
        Message::new(value.role().as_str(), value.content())
    }
}

/// An enum describing how the function output should be shown to the LLM.
#[derive(Debug, Copy, Clone, PartialEq)]
pub enum FunctionStyle {
    /// The function output is appended to the system prompt.
    SystemPrompt,

    /// The function output is provided via a message, using a role of "function".
    FunctionRole,

    /// The function output is appended to the last user message.
    AppendToUserMessage,
}

impl FunctionStyle {
    /// Returns true iff the instance is `SystemPrompt`.
    #[must_use]
    pub fn system_prompt(&self) -> bool {
        matches!(self, FunctionStyle::SystemPrompt)
    }

    /// Returns true iff the instance is `FunctionRole`.
    #[must_use]
    pub fn function_role(&self) -> bool {
        matches!(self, FunctionStyle::FunctionRole)
    }

    /// Returns true iff the instance is `AppendToUserMessage`.
    #[must_use]
    pub fn append_to_user_message(&self) -> bool {
        matches!(self, FunctionStyle::AppendToUserMessage)
    }
}

/// A struct representing a chat template.
pub struct ChatTemplate {
    template: String,
    bos_token: String,
    eos_token: String,

    /// If true, it means a 'nudge' is not needed/supported for the given template, and will be skipped during rendering.
    /// A 'nudge' is when we end a prompt with an empty assistant turn, and no eos, to nudge the LLM to produce a response.
    skip_nudge: bool,

    function_style: FunctionStyle,

    /// Some LLMs (Mistral-Instruct) don't have a concept of a "system" message;
    /// instead, they expect you to prepend the first user message with the system prompt
    combine_system_and_first_user_message: bool,
}

impl ChatTemplate {
    /// Create a new `ChatTemplate`.
    #[must_use]
    #[allow(clippy::similar_names)]
    pub fn new(
        template: &str,
        bos_token: &str,
        eos_token: &str,
        skip_nudge: bool,
        combine_system_and_first_user_message: bool,
        function_style: FunctionStyle,
    ) -> Self {
        Self {
            template: template.to_owned(),
            bos_token: bos_token.to_owned(),
            eos_token: eos_token.to_owned(),
            skip_nudge,
            combine_system_and_first_user_message,
            function_style,
        }
    }

    /// Gets the `bos_token`.
    #[must_use]
    pub fn bos_token(&self) -> &str {
        self.bos_token.as_ref()
    }

    /// Gets the `eos_token`.
    #[must_use]
    pub fn eos_token(&self) -> &str {
        self.eos_token.as_ref()
    }

    /// Gets the template.
    #[must_use]
    pub fn template(&self) -> &str {
        self.template.as_ref()
    }

    /// Gets a flag for `skip_nudge`.
    #[must_use]
    pub fn skip_nudge(&self) -> bool {
        self.skip_nudge
    }

    /// Gets the `FunctionStyle`.
    #[must_use]
    pub fn function_style(&self) -> FunctionStyle {
        self.function_style
    }

    /// Gets the flag that represents if the system message and first user message should be combined.
    #[must_use]
    pub fn combine_system_and_first_user_message(&self) -> bool {
        self.combine_system_and_first_user_message
    }
}

/// A struct to represent the logic for rendering a `History` into a `String`.
pub struct Renderer;

impl Renderer {
    /// Render the provided history into a String using the provided `ChatTemplate`.
    #[must_use]
    #[allow(clippy::missing_panics_doc)]
    pub fn render(history: &crate::history::History, template: &ChatTemplate) -> String {
        Self::render_with_settings(
            history,
            template,
            &RenderHistorySettingsBuilder::default().build().unwrap(),
        )
    }

    /// Render the provided history into a String using the provided `ChatTemplate` and settings.
    #[must_use]
    #[allow(clippy::too_many_lines, clippy::missing_panics_doc)]
    pub fn render_with_settings(
        history: &crate::history::History,
        template: &ChatTemplate,
        settings: &RenderHistorySettings,
    ) -> String {
        let environment = Environment::new();

        let skip_system = settings.skip_system();
        let add_assistant_nudge = settings.nudge_assistant();
        let function_style = template.function_style();
        let skip_functions =
            settings.skip_functions() || function_style != FunctionStyle::FunctionRole;

        let skip_count = {
            if skip_system && history.messages()[0].role() == &Role::System {
                1
            } else {
                0
            }
        };

        let messages: Vec<Message> = {
            let mut messages: Vec<Message> = history
                .messages()
                .iter()
                .skip(skip_count)
                .filter(|m| !skip_functions || m.role() != &Role::Function)
                .map(std::convert::Into::into)
                .collect();

            if !template.skip_nudge() && add_assistant_nudge {
                // Careful: if the template has an eos for assistant, then the nudge won't work
                messages.push(Message::new(Role::Assistant.as_str(), ""));
            }

            // Distance of last function from the end of conversation
            let last_function = history
                .messages()
                .iter()
                .rev()
                .position(|m| m.role() == &Role::Function);

            // Distance of last assistant from the end of conversation
            let last_assistant = messages
                .iter()
                .rev()
                .position(|m| m.role() == Role::Assistant.as_str())
                .unwrap_or(usize::MAX);

            if function_style.append_to_user_message() {
                // if there was a function call in the history...
                if last_function.is_some()
                // and if that function call is more recent than the last assistant message...
                && last_function.unwrap() < last_assistant
                {
                    // ...then we want to put the last function output in the last user message
                    let last_function_content = history
                        .messages()
                        .iter()
                        .rev()
                        .find(|m| m.role() == &Role::Function)
                        .unwrap()
                        .content()
                        .to_owned();

                    // ... update the last user's message to have the context
                    let last_user_message = messages
                        .iter_mut()
                        .rev()
                        .find(|m| m.role() == Role::User.as_str())
                        .unwrap();

                    let updated_content = format!(
                        "{}\n\n<context>{}\n</context>",
                        last_user_message.content(),
                        last_function_content
                    );

                    last_user_message.set_content(updated_content);
                }
            } else if function_style.system_prompt() {
                // Hear me out:
                // In the current if-block, we know we don't support the function role. So function output
                // has to go somewhere else.
                // if there was a function call in the history...
                if last_function.is_some()
                // and if that function call is more recent than the last assistant message...
                && last_function.unwrap() < last_assistant
                // and we're not skipping system messages...
                && !skip_system
                {
                    // ...then we want to put the last function output in the system prompt
                    let last_function_content = history
                        .messages()
                        .iter()
                        .rev()
                        .find(|m| m.role() == &Role::Function)
                        .unwrap()
                        .content()
                        .to_owned();

                    let first_message = messages.first_mut().unwrap();

                    if first_message.role() == Role::System.as_str() {
                        let content = first_message.content();

                        let updated_content = format!("{content}\n\nThe following contextual info may help respond to the user:\n\n<context>\n{last_function_content}\n</context>");

                        first_message.set_content(updated_content);
                    }
                }
            }

            if template.combine_system_and_first_user_message() {
                if let Some(first_message) = messages.first() {
                    if first_message.role() == Role::System.as_str() {
                        let system_message_content = first_message.content().to_owned();

                        // Remove the system message, since the template says it is not supported
                        messages.remove(0);

                        let first_user_message = messages
                            .first_mut()
                            .expect("Expected another message after system message");

                        assert!(first_user_message.role() == Role::User.as_str(), "Expected the first message after the system message to be a user message");

                        // Prepend the first user message, which is now the very first message, with the old system prompt.
                        let combined = format!(
                            "{system_message_content}\n\n{}",
                            first_user_message.content()
                        );

                        first_user_message.set_content(combined);
                    }
                }
            }

            messages
        };

        let history = History::new(
            messages,
            template.bos_token(),
            template.eos_token(),
            // If we are nudging the assistant, skip the final eos. For templates that don't support this, it's a noop anyway.
            add_assistant_nudge,
        );

        environment
            .render_str(template.template(), history)
            .unwrap()
    }
}
