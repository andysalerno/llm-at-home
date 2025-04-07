from chat_loop import run_loop
from agents.responding_assistant import create_responding_assistant
from state import State
import asyncio
import logging

logger = logging.getLogger(__name__)


def _configure_phoenix():
    from phoenix.otel import register
    from openinference.instrumentation.openai import OpenAIInstrumentor

    register()
    OpenAIInstrumentor().instrument()


async def main():
    _configure_phoenix()

    agent = create_responding_assistant()
    state = State()

    logger.info("Starting chat loop...")
    await run_loop(agent, state)


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)

    asyncio.run(main())
