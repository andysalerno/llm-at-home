from pydantic_ai import Agent
from pydantic_ai.tools import Tool
from pydantic_ai.settings import ModelSettings
from pydantic_ai.common_tools.duckduckgo import duckduckgo_search_tool
from jinja2 import Template
import textwrap
import datetime
from code_execution_tool import create_code_execution_tool
from model import create_model, get_instrumentation_settings
from research_agent import research_agent_tool
from state import State
from wiki_tool import create_wiki_tool
import asyncio


def _configure_phoenix():
    from phoenix.otel import register
    from openinference.instrumentation.openai import OpenAIInstrumentor

    register()
    OpenAIInstrumentor().instrument()


async def main():
    _configure_phoenix()

    instrumentation_settings = get_instrumentation_settings()

    cur_date = datetime.datetime.now().strftime("%Y-%m-%d")

    agent = Agent(
        model=create_model(),
        deps_type=State,
        tools=[  # type: ignore
            # duckduckgo_search_tool(),
            # create_wiki_tool(),
            # create_code_execution_tool(),
            research_agent_tool(),  # type: ignore
        ],
        model_settings=ModelSettings(
            temperature=0.0,
        ),
        system_prompt=_create_prompt(
            [
                research_agent_tool(),
                # duckduckgo_search_tool(),
                # create_wiki_tool(),
                # create_code_execution_tool(),
            ],
            cur_date,
        ),
    )

    agent.instrument_all(instrumentation_settings)

    message_history = None

    aggregate_usage = None

    state = State()

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


def _create_prompt(tools: list[Tool], date_str: str) -> str:
    return Template(
        textwrap.dedent("""\
        You are a helpful assistant. Help the user as best you can.

        Since your internal knowledge is limited, you may invoke the following tools to get more information (including up-to-date information):
        {%- for tool in tools %}
        - {{ tool.name }}
          - {{ tool.description }}
        {%- endfor %}
                        
        ## Additional context
        The current date is: {{ date_str }}.

        ## Additional rules
        - Always prefer to use tools over your own knowledge. Even when you think you know the answer, it is better to use a tool to get the most accurate and up-to-date information, and to discover sources to provide to the user.
        - If you perform a search, and the search results are not relevant, you should try an entirely different search query, or try a different tool.
        - If you still cannot find a relevant result, tell the user you do not know.
        - If you need to do any kind of calculation, prefer the Python code execution tool; Python is better at math than you are! 
        """).strip()
    ).render(tools=tools, date_str=date_str)


if __name__ == "__main__":
    asyncio.run(main())
