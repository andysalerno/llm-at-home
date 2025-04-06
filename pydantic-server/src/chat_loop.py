from typing import Any
from pydantic_ai import Agent
from state import State


async def run_loop(agent: Agent[Any], starting_state: State):
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

    print(f"Final count: {aggregate_usage}")
