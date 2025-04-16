import logging
from typing import Any

from pydantic_ai import Agent
from pydantic_ai.messages import (
    ModelMessage,
    ModelRequest,
    ModelRequestPart,
    ToolReturnPart,
)

from state import State

logger = logging.getLogger(__name__)


async def run_loop(
    agent: Agent[Any],
    starting_state: State,
    trim_old_tool_outputs: bool = False,
) -> None:
    state = starting_state
    aggregate_usage = None

    while True:
        try:
            user_input = input("You: ")
        except KeyboardInterrupt:
            logger.info("\nExiting...")
            break

        if user_input.lower() in ["/exit", "/quit"]:
            break

        # Run the agent with the user input
        response = await agent.run(
            user_input,
            message_history=state.message_history
            if len(state.message_history) > 0
            else None,
            deps=state,
        )

        if trim_old_tool_outputs:
            next_history = _trim_old_tool_outputs(response.all_messages())
        else:
            next_history = response.all_messages()

        state.message_history = next_history
        logger.info(response.data)

        if aggregate_usage is None:
            aggregate_usage = response.usage()
        else:
            aggregate_usage.incr(response.usage())

        logger.info(response.usage())
        logger.info("Combined: %s", aggregate_usage)

    logger.info("Final count: %s", aggregate_usage)


def _trim_old_tool_outputs(
    history: list[ModelMessage],
    max_len_per_output: int = 200,
) -> list[ModelMessage]:
    def transform_message(msg: ModelMessage) -> ModelMessage:
        if isinstance(msg, ModelRequest):
            return ModelRequest(
                parts=[transform_part(part) for part in msg.parts],
            )

        return msg

    def transform_part(part: ModelRequestPart) -> ModelRequestPart:
        if isinstance(part, ToolReturnPart):
            trimmed_content = part.content
            if len(trimmed_content) > max_len_per_output:
                trimmed_content = part.content[:max_len_per_output] + "...(truncated)"
            return ToolReturnPart(
                tool_name=part.tool_name,
                content=trimmed_content,
                tool_call_id=part.tool_call_id,
                timestamp=part.timestamp,
            )

        return part

    return [transform_message(msg) for msg in history]
