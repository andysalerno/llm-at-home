import logging
from typing import Any

from pydantic_ai import Agent

from state import State

logger = logging.getLogger(__name__)


async def run_loop(agent: Agent[Any], starting_state: State) -> None:
    state = starting_state
    message_history = None
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
            message_history=message_history,
            deps=state,
        )
        message_history = response.all_messages()
        state.message_history = message_history
        logger.info(response.data)

        if aggregate_usage is None:
            aggregate_usage = response.usage()
        else:
            aggregate_usage.incr(response.usage())

        logger.info(response.usage())
        logger.info("Combined: %s", aggregate_usage)

    logger.info("Final count: %s", aggregate_usage)
