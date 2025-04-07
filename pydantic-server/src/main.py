import asyncio
import logging

from agents.responding_assistant import create_responding_assistant
from chat_loop import run_loop
from state import State
from tools.memory_tool import store_memory_tool

logger = logging.getLogger(__name__)


def _configure_phoenix() -> None:
    from openinference.instrumentation.openai import OpenAIInstrumentor
    from phoenix.otel import register

    register()
    OpenAIInstrumentor().instrument()


async def main() -> None:
    _configure_phoenix()

    agent = create_responding_assistant(extra_tools=[store_memory_tool()])
    state = State()

    logger.info("Starting chat loop...")
    await run_loop(agent, state)


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)

    asyncio.run(main())
