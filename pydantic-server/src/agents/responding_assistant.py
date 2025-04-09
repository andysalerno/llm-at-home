import datetime
import textwrap
from typing import Any

from jinja2 import Template
from pydantic_ai import Agent, Tool
from pydantic_ai.settings import ModelSettings

from agents.research_agent import research_agent_tool
from model import create_model, get_instrumentation_settings
from state import State


def create_responding_assistant(
    temperature: float = 0.2,
    extra_tools: list[Tool[Any]] | None = None,
    include_tools_in_prompt: bool = True,
) -> Agent[State, str]:
    if extra_tools is None:
        extra_tools = []
    instrumentation_settings = get_instrumentation_settings()
    cur_date = datetime.datetime.now().strftime("%Y-%m-%d")

    tools = [research_agent_tool(include_tools_in_prompt), *extra_tools]

    agent = Agent(
        model=create_model(),
        deps_type=State,
        tools=tools,
        model_settings=ModelSettings(
            temperature=temperature,
        ),
        system_prompt=_create_prompt(tools, cur_date, include_tools_in_prompt),
    )

    agent.instrument_all(instrumentation_settings)

    return agent


def _create_prompt(
    tools: list[Tool],
    date_str: str,
    include_tools_in_prompt: bool,
) -> str:
    return Template(
        textwrap.dedent("""\
        You are a helpful assistant. Help the user as best you can.

        Since your internal knowledge is limited, you may invoke tools to get more information (including up-to-date information).

        {%- if include_tools_in_prompt %}
        The following tools are available to you:
        {%- for tool in tools %}
        - {{ tool.name }}
          - {{ tool.description }}
        {%- endfor %}
        {%- endif %}

        ## Additional context
        The current date is: {{ date_str }}.

        ## Additional rules
        - Always prefer to use the researcher over your own knowledge. Even when you think you know the answer, it is better to use the researcher tool to get the most accurate and up-to-date information, and to discover sources to provide to the user.
        - If you still cannot find a relevant result, even after invoking the researcher, tell the user you do not know.
        - If you need to do any kind of calculation, delegate to the researcher; it is better at math than you are!
        - The research assistant may provide more information than necessary to handle the user's question. In that case, provide whatever extra context or information that you think might be useful to the user.
        - You must NOT include tool calls alongside messages to the user. Your response must be EITHER tool invocations OR a message to the user, but not both.
        """).strip(),
    ).render(
        tools=tools,
        date_str=date_str,
        include_tools_in_prompt=include_tools_in_prompt,
    )
