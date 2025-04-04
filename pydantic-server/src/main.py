from typing import Any
from pydantic_ai import Agent
from responding_assistant import create_responding_assistant
from state import State
import asyncio


def _configure_phoenix():
    from phoenix.otel import register
    from openinference.instrumentation.openai import OpenAIInstrumentor

    register()
    OpenAIInstrumentor().instrument()


async def main():
    _configure_phoenix()

    agent = create_responding_assistant()
    state = State()

    await _run_loop(agent, state)


async def _run_loop(agent: Agent[Any], starting_state: State):
    state = starting_state
    message_history = None
    aggregate_usage = None

    while True:
        try:
            user_input = input("You: ")
        except KeyboardInterrupt:
            print("\nExiting...")
            break

        if user_input.lower() in ["/exit", "/quit"]:
            break

        # Run the agent with the user input
        response = await agent.run(
            user_input, message_history=message_history, deps=state
        )
        message_history = response.all_messages()
        state.message_history = message_history
        print(response.data)

        if aggregate_usage is None:
            aggregate_usage = response.usage()
        else:
            aggregate_usage.incr(response.usage())

        print(response.usage())
        print(f"Combined: {aggregate_usage}")

        print(
            f"============Full Trace============\n{response.new_messages()}\n=================================="
        )

    print(f"Final count: {aggregate_usage}")


if __name__ == "__main__":
    asyncio.run(main())
