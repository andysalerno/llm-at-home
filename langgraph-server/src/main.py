import os
import json
import asyncio
from langgraph.prebuilt import create_react_agent
from langchain_openai import ChatOpenAI
from pydantic import SecretStr
from langgraph.graph.state import CompiledStateGraph
from langchain_core.messages import BaseMessage, ChatMessage
from duckduckgo_search import DDGS

# from langchain_community.tools.ddg_search import DuckDuckGoSearchRun
from pydantic_ai.common_tools.duckduckgo import DuckDuckGoSearchTool

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

        # user_message = {"role": "user", "content": user_input}

        conversation_history["messages"].append(
            # BaseMessage(content=user_input, role="user")
            ChatMessage(content=user_input, role="user")
        )

        for event in graph.stream(conversation_history):
            for value in event.values():
                assistant_message = value["messages"][-1]
                assert isinstance(assistant_message, BaseMessage), (
                    f"Expected BaseMessage but got {type(assistant_message)}"
                )
                conversation_history["messages"].append(assistant_message)
                print("Message:", assistant_message.content)


def search_web(query: str) -> str:
    """
    Searches the web using Google and returns the results.

    Args:
        query: The google query.
    """
    tool = DuckDuckGoSearchTool(DDGS())
    results = asyncio.run(tool(query))
    return json.dumps(results)


def main():
    _configure_phoenix()

    # agent_graph = create_react_agent(create_model(), tools=[get_weather])
    agent_graph = create_custom_react_agent(
        create_model(), tools=[get_weather, tallest_building_faq, search_web]
    )
    # agent_graph = create_simple_chat_agent(create_model())

    print(agent_graph.get_graph().draw_mermaid())

    _run_chat_loop(agent_graph)

    pass


if __name__ == "__main__":
    main()
