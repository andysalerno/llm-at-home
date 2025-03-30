import os
from langgraph.prebuilt import create_react_agent
from langchain_openai import ChatOpenAI
from pydantic import SecretStr
from langgraph.graph.state import CompiledStateGraph

from custom_react import ChatState, create_custom_react_agent, create_simple_chat_agent


def _configure_phoenix():
    from phoenix.otel import register

    # configure the Phoenix tracer
    register(
        auto_instrument=True,  # Auto-instrument your app based on installed OI dependencies
    )


def create_model():
    api_key = os.environ.get("LLM_API_KEY")

    model_name = os.environ.get("MODEL_NAME")
    print(f"using model: {model_name}")

    if not api_key:
        raise ValueError("LLM_API_KEY environment variable not set")
    if not model_name:
        raise ValueError("MODEL_NAME environment variable not set")

    api_key = SecretStr(api_key)

    model = ChatOpenAI(
        base_url="https://openrouter.ai/api/v1",
        model=model_name,
        api_key=api_key,
        temperature=0.0,
    )

    return model


def get_weather(location: str) -> str:
    """Gets the weather for a given location."""
    if "sf" in location.lower() or "san francisco" in location.lower():
        return "It's 60 degrees and foggy."
    return "It's 90 degrees and sunny."


def tallest_building_faq(location: str) -> str:
    """Gets info on the tallest building in a given location, including its height."""
    return "The tallest building in Seattle is the Columbia Center, which is 967 feet tall."


def _run_chat_loop(graph: CompiledStateGraph):
    conversation_history: ChatState = {"messages": []}

    while True:
        user_input = input("User: ")
        if user_input.lower() in ["quit", "exit", "q"]:
            print("Goodbye!")
            break

        user_message = {"role": "user", "content": user_input}

        conversation_history["messages"].append(user_message)

        for event in graph.stream(conversation_history):
            for value in event.values():
                assistant_message = value["messages"][-1]
                conversation_history["messages"].append(assistant_message)
                print("Assistant:", value["messages"][-1])


def main():
    _configure_phoenix()

    # agent_graph = create_react_agent(create_model(), tools=[get_weather])
    agent_graph = create_custom_react_agent(
        create_model(), tools=[get_weather, tallest_building_faq]
    )
    # agent_graph = create_simple_chat_agent(create_model())

    print(agent_graph.get_graph().draw_mermaid())

    _run_chat_loop(agent_graph)

    pass


if __name__ == "__main__":
    main()
