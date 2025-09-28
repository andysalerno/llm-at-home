import datetime
import logging
import textwrap
from typing import Any

from agents import Agent, ModelSettings
from agents.mcp import MCPServer
from agents.tool import Tool
from jinja2 import Template

from agent_definitions.reason_tool import reason
from config import config
from mcp_registry import get_named_server
from model import get_model

logger = logging.getLogger(__name__)

_calculator_description = (
    "Gives a task to a math calculator agent and returns its final result."
    ' Tasks are in natural language and can be anything from "What is 123 + 456?" to "How many days were there between July 4, 1776 and December 25, 2023?"'
    " Tasks can be simple or complex, but MUST be calculation-related; this agent knows nothing about the world beyond calculations."
)


async def calculator_agent_tool(
    agent_temp: float = 0.0,
    top_p: float = 0.9,
    mcp_server: MCPServer | None = None,
) -> Tool:
    agent = await create_calculator_agent(agent_temp, top_p, mcp_server)

    return agent.as_tool(
        tool_name="ask_calculator_agent",
        tool_description=_calculator_description,
    )


async def create_calculator_agent(
    temp: float,
    top_p: float,
    mcp_server: MCPServer | None = None,
) -> Agent[Any]:
    cur_date = _get_now_str()

    mcp_server = get_named_server("calculator")

    tools = []

    if config.ENABLE_REASON_TOOL:
        tools.append(reason)

    return Agent(
        name="CalculatorAgent",
        handoff_description=_calculator_description,
        model=get_model(),
        mcp_servers=[mcp_server],
        # output_type=ResearchComplete, # breaks in vllm and llamacpp
        handoffs=[],
        model_settings=ModelSettings(
            top_p=top_p,
            temperature=temp,
            parallel_tool_calls=config.PARALLEL_TOOL_CALLS,
        ),
        instructions=_create_prompt(
            cur_date,
        ),
    )


def _get_now_str() -> str:
    return datetime.datetime.now().strftime("%Y-%m-%d")


def _create_prompt(
    date_str: str,
    max_tool_calls: int = 5,
) -> str:
    return Template(
        textwrap.dedent("""\
        You are a calculator agent. Perform the user's calculation-related task and respond with the result.

        Your calculations are handled primarily by invoking a Python interpreter tool.

        ## Rules
        - You are limited to at most **{{ max_tool_calls }}** total tool invocations during this task (since the last user message).
        - After invoking at most **{{ max_tool_calls }}** tools, you must then respond.

        ## Additional context
        The current date is: {{ date_str }}.
        """).strip(),
    ).render(
        date_str=date_str,
        max_tool_calls=max_tool_calls,
    )
