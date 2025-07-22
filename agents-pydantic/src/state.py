from dataclasses import dataclass, field

from pydantic_ai.messages import ModelMessage, ModelRequest, SystemPromptPart


@dataclass
class State:
    message_history: list[ModelMessage] = field(default_factory=list)

    def without_system_prompt(self) -> "State":
        return State(
            message_history=[
                self._model_request_without_system_prompt(msg)
                for msg in self.message_history
            ],
        )

    def with_system_prompt_prepended(self, system_prompt: str) -> "State":
        message_history = self.message_history[:]
        message_history.insert(0, ModelRequest(parts=[SystemPromptPart(system_prompt)]))

        return State(message_history=message_history)

    def with_system_prompt_replaced(self, system_prompt: str) -> "State":
        without_system_prompt = self.without_system_prompt()
        return without_system_prompt.with_system_prompt_prepended(system_prompt)

    def _model_request_without_system_prompt(self, msg: ModelMessage) -> ModelMessage:
        """Returns a new ModelRequest without the system prompt part."""
        if isinstance(msg, ModelRequest):
            return ModelRequest(
                parts=[
                    part for part in msg.parts if not isinstance(part, SystemPromptPart)
                ],
            )

        return msg
