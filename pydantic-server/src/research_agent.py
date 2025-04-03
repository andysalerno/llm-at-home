import textwrap
from jinja2 import Template
from pydantic_ai import Agent, RunContext
from pydantic_ai.tools import Tool
from pydantic_ai.settings import ModelSettings
from pydantic_ai.common_tools.duckduckgo import duckduckgo_search_tool
import datetime
from model import create_model
from state import State
from wiki_tool import create_wiki_tool


async def _run_research_agent(task: str, ctx: RunContext[State]) -> str:
    """
    Gives a task to a research agent and returns the final result of the research.
    Tasks are in natural language and can be anything from "What is the capital of France?" to "Write a Python script that calculates the Fibonacci sequence."
    Tasks can be simple or complex.
    The research agent will return a report with the results of the research.

    Args:
        task: The task to be performed by the research agent.
    """
    agent = create_agent()

    result = await agent.run(task, deps=ctx.deps)

    return result.data


def research_agent_tool() -> Tool:
    return Tool(
        function=_run_research_agent,
        name="perform_research",
    )


def create_agent():
    cur_date = datetime.datetime.now().strftime("%Y-%m-%d")

    agent = Agent(
        model=create_model(),
        deps_type=State,
        tools=[
            duckduckgo_search_tool(),  # type: ignore
        ],
        model_settings=ModelSettings(
            temperature=0.0,
        ),
        system_prompt=_create_prompt(
            [duckduckgo_search_tool(), create_wiki_tool()], cur_date
        ),
    )

    return agent


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
        """).strip()
    ).render(tools=tools, date_str=date_str)
