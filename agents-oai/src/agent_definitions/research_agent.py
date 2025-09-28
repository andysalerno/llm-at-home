import datetime
import logging
import textwrap
from typing import Any

from jinja2 import Template
from pydantic import BaseModel
from agents.mcp import MCPServer

from agents import Agent, ModelSettings, handoff
from model import get_model
from agents.tool import Tool
from agent_definitions.reason_tool import reason
from config import config
from agent_definitions.math_agent import calculator_agent_tool, create_calculator_agent

logger = logging.getLogger(__name__)


_description = (
    "Gives a task to a research agent and returns the final result of its research."
    ' Tasks are in natural language and can be anything from "What is the capital of France?" to "Write a Python script that calculates the Fibonacci sequence."'
    " Tasks can be simple or complex."
    " The research agent will return a report with the results of the research, including relevant documents."
)


# TODO: add a flag to force the agent to remove
# existing tool calls and outputs before running
async def research_agent_tool(
    agent_temp: float = 0.0, top_p: float = 0.9, mcp_server: MCPServer | None = None
) -> Tool:
    agent = await create_research_agent(agent_temp, top_p, mcp_server)

    return agent.as_tool(tool_name="ask_researcher", tool_description=_description)


class ResearchComplete(BaseModel):
    handoff_message: str


async def create_research_agent(
    temp: float,
    top_p: float,
    mcp_server: MCPServer | None = None,
) -> Agent[Any]:
    cur_date = _get_now_str()

    mcp_servers = [mcp_server] if mcp_server else []

    tools = [] if not config.ENABLE_REASON_TOOL else [reason]
    handoffs = []

    if not config.USE_HANDOFFS:
        tools.append(await calculator_agent_tool(temp, top_p, mcp_server))
    else:
        handoffs.append(handoff(await create_calculator_agent(temp, top_p, mcp_server)))

    return Agent(
        name="ResearchAgent",
        handoff_description=_description,
        handoffs=handoffs,
        model=get_model(),
        tools=tools,  # type: ignore
        # output_type=ResearchComplete, # breaks in vllm and llamacpp
        mcp_servers=mcp_servers,
        model_settings=ModelSettings(
            top_p=top_p,
            temperature=temp,
            parallel_tool_calls=config.PARALLEL_TOOL_CALLS,
            tool_choice="reason" if config.ENABLE_REASON_TOOL else "auto",
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
        You are a research assistant. Perform research to accomplish the given task.

        Research is performed by querying the web and visiting relevant web pages.

        ## Rules
        - You MUST invoke tools, *one at a time*, to gather information related to your task.
        {{ reason_tool_details -}}
        - You are limited to at most **{{ max_tool_calls }}** total tool invocations during this task (since the last user message).
        - After invoking at most **{{ max_tool_calls }}** tools, you must then respond.
        - After searching the web and getting relevant urls, use the `visit_url` tool to scrape them and acquire their information.
        - If you invoke a tool but it does not provide the information you need, you MAY invoke the same tool again with a different query.
        - ALWAYS perform at least one tool invocation before responding.
        - Cite all sources of information you use in your final response.
        - Do NOT use your own knowledge, only use the information you gather from the tools you invoke.

        ## Definition of done
        Your research is complete when you have gathered sufficient information to respond to the task.
        At that point, simply respond with your answer.
        OR, if you have performed more than **{{ max_tool_calls }}** tool invocations, and have still not found sufficient information, you respond with what you have found and indicate you could not find sufficient information.

        ## Citations
        Provided the list of sources you used as markdown links at the end of your response.
        The markdown link must be in the format `[title](http://url)`. You can't use numbered links like [1] or [2] or other shorthand, those are not supported.

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
