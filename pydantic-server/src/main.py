from pydantic_ai import Agent
from pydantic_ai.tools import Tool
from pydantic_ai.settings import ModelSettings
from pydantic_ai.common_tools.duckduckgo import duckduckgo_search_tool
from jinja2 import Template
import textwrap
import datetime
from code_execution_tool import create_code_execution_tool
from model import create_model, get_instrumentation_settings
from wiki_tool import create_wiki_tool


def _configure_phoenix():
    from phoenix.otel import register
    from openinference.instrumentation.openai import OpenAIInstrumentor

    register()
    OpenAIInstrumentor().instrument()


def main():
    _configure_phoenix()

    instrumentation_settings = get_instrumentation_settings()

    cur_date = datetime.datetime.now().strftime("%Y-%m-%d")

    agent = Agent(
        model=create_model(),
        tools=[
            duckduckgo_search_tool(),
            create_wiki_tool(),
            create_code_execution_tool(),
        ],
        model_settings=ModelSettings(
            temperature=0.0,
        ),
        system_prompt=_create_prompt(
            [
                duckduckgo_search_tool(),
                create_wiki_tool(),
                create_code_execution_tool(),
            ],
            cur_date,
        ),
    )

    agent.instrument_all(instrumentation_settings)

    message_history = None

    while True:
        user_input = input("You: ")
        if user_input.lower() in ["/exit", "/quit"]:
            break

        # Run the agent with the user input
        response = agent.run_sync(user_input, message_history=message_history)
        message_history = response.all_messages()
        print(response.data)
        print(
            f"============Full Trace============\n{response.new_messages()}\n========================"
        )


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
        Always prefer to use tools over your own knowledge. Even when you think you know the answer, it is better to use a tool to get the most accurate and up-to-date information, and to discover sources to provide to the user.
        If you perform a search, and the search results are not relevant, you should try an entirely different search query, or try a different tool.
        If you still cannot find a relevant result, tell the user you do not know.
        """).strip()
    ).render(tools=tools, date_str=date_str)


if __name__ == "__main__":
    main()
