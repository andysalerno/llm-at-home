from dataclasses import dataclass, field
from pydantic_ai.messages import ModelMessage


@dataclass
class State:
    message_history: list[ModelMessage] = field(default_factory=list)
