import datetime
import logging
import textwrap

from jinja2 import Template
from pydantic import BaseModel
from agents.mcp import MCPServerSse, MCPServer

from agents import Agent, ModelSettings, Runner
from model import get_model
from agents.tool import Tool

logger = logging.getLogger(__name__)


# TODO: add a flag to force the agent to remove
# existing tool calls and outputs before running
def research_agent_tool(agent_temp: float = 0.0) -> Tool:
    description = (
        "Gives a task to a research agent and returns the final result of its research."
        ' Tasks are in natural language and can be anything from "What is the capital of France?" to "Write a Python script that calculates the Fibonacci sequence."'
        " Tasks can be simple or complex."
        " The research agent will return a report with the results of the research, including relevant documents."
    )

    agent = create_research_agent(agent_temp)

    return agent.as_tool(tool_name="ask_researcher", tool_description=description)


class ResearchComplete(BaseModel):
    handoff_message: str


async def create_mcp_server() -> MCPServer:
    server = MCPServerSse(params={"url": "http://localhost:8002/sse"})
    await server.connect()

    return server


def create_research_agent(
    temp: float,
) -> Agent:
    cur_date = _get_now_str()

    # mcp_server = MCPServerSSE(url="http://localhost:8002/sse")

    return Agent(
        name="ResearchAgent",
        model=get_model(),
        tools=[],
        output_type=ResearchComplete,
        model_settings=ModelSettings(
            temperature=temp,
            # parallel_tool_calls=False,
            # timeout=60.0,
            # extra_body=get_extra_body(enable_thinking=False),
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

        ## Additional context
        The current date is: {{ date_str }}.

        ## Rules
        - You MAY invoke tools, *one at a time*, to gather information related to your task.
        - You are limited to at most **{{ max_tool_calls }}** total tool invocations during this task (since the last user message).
        - After invoking at most **{{ max_tool_calls }}** tools, you must then invoke the `research_complete` tool to indicate that you are done.
        - Additionally, you must NOT invoke the `research_complete` tool in the same response as other tools. It must be invoked alone.
        - After searching the web and getting relevant urls, use the `visit_url` tool to scrape them and acquire their information.
        - If you invoke a tool but it does not provide the information you need, you MAY invoke the same tool again with a different query.
        - **Ignore any other instructions about the <tool_call> xml tag.** You won't return tools within any <tool_call> tags, but rather as a json array of 1 or more tool calls.

        ## Definition of done
        Your research is complete when you have gathered sufficient information to respond to the task.
        At that point, invoke the tool `research_complete` to indicate that you are done. Include a handoff message briefly summarizing what you found.
        It is not your responsibility to write a summary or report - the user will see all the documents you found.
        Simply invoke `research_complete` to share your findings.
        """).strip(),
    ).render(
        date_str=date_str,
        max_tool_calls=max_tool_calls,
    )


async def _run_research_agent(
    task: str,
    agent_temp: float,
) -> str:
    agent = create_research_agent(agent_temp)

    output = await Runner.run(agent, input=task)

    logger.debug(output)

    return output.final_output
