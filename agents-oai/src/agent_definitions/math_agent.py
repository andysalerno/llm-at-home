import datetime
import logging
import textwrap
from typing import Any

from jinja2 import Template
from agents.mcp import MCPServer

from agents import Agent, ModelSettings
from model import get_model
from agents.tool import Tool
from agent_definitions.reason_tool import reason
from config import config

logger = logging.getLogger(__name__)

_calculator_description = (
    "Gives a task to a math calculator agent and returns its final result."
    ' Tasks are in natural language and can be anything from "What is 123 + 456?" to "How many days were there between July 4, 1776 and December 25, 2023?"'
    " Tasks can be simple or complex, but MUST be calculation-related; this agent knows nothing about the world beyond calculations."
)


async def calculator_agent_tool(
    agent_temp: float = 0.0, top_p: float = 0.9, mcp_server: MCPServer | None = None
) -> Tool:
    agent = await create_calculator_agent(agent_temp, top_p, mcp_server)

    return agent.as_tool(
        tool_name="ask_calculator_agent", tool_description=_calculator_description
    )


async def create_calculator_agent(
    temp: float,
    top_p: float,
    mcp_server: MCPServer | None = None,
) -> Agent[Any]:
    cur_date = _get_now_str()

    tools = []
    if mcp_server:
        python_tool = next(
            (
                t
                for t in await mcp_server.list_tools()
                if t.name == "execute_python_code"
            ),
            None,
        )
        if python_tool:
            logger.info("Adding execute_python_code tool to CalculatorAgent")
        else:
            logger.warning("MCP server provided but execute_python_code tool not found")

    if config.ENABLE_REASON_TOOL:
        tools.append(reason)

    return Agent(
        name="CalculatorAgent",
        handoff_description=_calculator_description,
        model=get_model(),
        tools=tools,  # type: ignore
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
        {{ reason_tool_details -}}
        - You are limited to at most **{{ max_tool_calls }}** total tool invocations during this task (since the last user message).
        - After invoking at most **{{ max_tool_calls }}** tools, you must then respond.

        ## Additional context
        The current date is: {{ date_str }}.
        """).strip(),
    ).render(
        date_str=date_str,
        max_tool_calls=max_tool_calls,
        reason_tool_details="- You MUST invoke the `reason` tool to record your thought process and plan before invoking any other tool.\n- You MUST NOT invoke ANY tool (even subsequent tools) unless you first invoked the `reason` tool to record your thoughts.\n"
        if config.ENABLE_REASON_TOOL
        else "",
    )
