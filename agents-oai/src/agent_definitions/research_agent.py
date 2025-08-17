import datetime
import logging
import textwrap
from typing import Any

from jinja2 import Template
from pydantic import BaseModel
from agents.mcp import MCPServer

from agents import Agent, ModelSettings, Runner
from model import get_model
from agents.tool import Tool

logger = logging.getLogger(__name__)


# TODO: add a flag to force the agent to remove
# existing tool calls and outputs before running
async def research_agent_tool(
    agent_temp: float = 0.0, mcp_server: MCPServer | None = None
) -> Tool:
    description = (
        "Gives a task to a research agent and returns the final result of its research."
        ' Tasks are in natural language and can be anything from "What is the capital of France?" to "Write a Python script that calculates the Fibonacci sequence."'
        " Tasks can be simple or complex."
        " The research agent will return a report with the results of the research, including relevant documents."
    )

    agent = await create_research_agent(agent_temp, mcp_server)

    return agent.as_tool(tool_name="ask_researcher", tool_description=description)


class ResearchComplete(BaseModel):
    handoff_message: str


async def create_research_agent(
    temp: float,
    mcp_server: MCPServer | None = None,
) -> Agent[Any]:
    cur_date = _get_now_str()

    mcp_servers = [mcp_server] if mcp_server else []

    return Agent(
        name="ResearchAgent",
        model=get_model(),
        tools=[],
        # output_type=ResearchComplete, # breaks in vllm and llamacpp
        mcp_servers=mcp_servers,
        handoffs=[],
        model_settings=ModelSettings(
            temperature=temp,
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
        - You are limited to at most **{{ max_tool_calls }}** total tool invocations during this task (since the last user message).
        - After invoking at most **{{ max_tool_calls }}** tools, you must then respond.
        - After searching the web and getting relevant urls, use the `visit_url` tool to scrape them and acquire their information.
        - If you invoke a tool but it does not provide the information you need, you MAY invoke the same tool again with a different query.
        - Cite all sources of information you use in your final response.

        ## Definition of done
        Your research is complete when you have gathered sufficient information to respond to the task.
        At that point, simply respond with your answer.

        ## Citations
        Provided the list of sources you used as markdown links at the end of your response.
        The markdown link must be in the format `[title](url)`. You can't use numbered links like [1] or [2], those are not supported.

        ## Additional context
        The current date is: {{ date_str }}.
        """).strip(),
    ).render(
        date_str=date_str,
        max_tool_calls=max_tool_calls,
    )


async def _run_research_agent(
    task: str,
    agent_temp: float,
) -> str:
    agent = await create_research_agent(agent_temp)

    output = await Runner.run(agent, input=task)

    logger.debug(output)

    return output.final_output
