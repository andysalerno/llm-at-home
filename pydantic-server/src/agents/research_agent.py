import datetime
import json
import logging
import textwrap
from typing import Any

from jinja2 import Template
from pydantic import BaseModel
from pydantic_ai import Agent, RunContext
from pydantic_ai.messages import (
    ModelMessage,
    ModelRequest,
    ModelResponse,
    ToolCallPart,
    ToolReturnPart,
)
from pydantic_ai.settings import ModelSettings
from pydantic_ai.tools import Tool

from model import create_model
from state import State
from tools.code_execution_tool import create_code_execution_tool
from tools.google_search_tool import create_google_search_tool
from tools.visit_url_tool import create_visit_site_tool
from tools.wiki_tool import create_wiki_tool

logger = logging.getLogger(__name__)


# TODO: add a flag to force the agent to remove
# existing tool calls and outputs before running
def research_agent_tool(include_tools_in_prompt: bool) -> Tool:
    async def _run(ctx: RunContext[State], task: str) -> str:
        return await _run_research_agent(ctx, task, include_tools_in_prompt)

    description = (
        "Gives a task to a research agent and returns the final result of the research."
        ' Tasks are in natural language and can be anything from "What is the capital of France?" to "Write a Python script that calculates the Fibonacci sequence."'
        " Tasks can be simple or complex."
        " The research agent will return a report with the results of the research."
    )

    return Tool(
        function=_run,
        name="perform_research",
        description=description,
        takes_ctx=True,
    )


async def _run_research_agent(
    ctx: RunContext[State],
    task: str,
    include_tools_in_prompt: bool,
) -> str:
    """
    Gives a task to a research agent and returns the final result of the research.
    Tasks are in natural language and can be anything from "What is the capital of France?" to "Write a Python script that calculates the Fibonacci sequence."
    Tasks can be simple or complex.
    The research agent will return a report with the results of the research.

    Args:
        task: The task to be performed by the research agent.
    """
    agent = _create_agent(include_tools_in_prompt)

    system_prompt = _create_prompt_with_default_tools(
        _get_now_str(), include_tools_in_prompt
    )
    state: State = ctx.deps.with_system_prompt_replaced(system_prompt)

    result = await agent.run(task, message_history=state.message_history)

    logger.info(result.new_messages())

    tool_outputs = _extract_tool_return_parts(result.new_messages())

    return json.dumps(tool_outputs, indent=2)


def _extract_tool_return_parts(
    message_history: list[ModelMessage],
) -> list[dict[str, str]]:
    """Extracts the ToolReturnParts from the message history."""
    outputs = [
        {"tool_name": part.tool_name, "result": part.content}
        for msg in message_history
        if isinstance(msg, ModelRequest)
        for part in msg.parts
        if isinstance(part, ToolReturnPart)
    ]

    # find the handoff_message, which is actually an arg on ToolCallPart, not a ToolReturnPart
    handoff_message = [
        {
            "tool_name": part.tool_name,
            "result": part.args_as_dict().get("handoff_message"),
        }
        for msg in message_history
        if isinstance(msg, ModelResponse)
        for part in msg.parts
        if isinstance(part, ToolCallPart)
    ]

    # get last instance of handoff_message, if it exists:
    if handoff_message:
        handoff_message = handoff_message[-1]
        outputs.append(handoff_message)

    return outputs


class ResearchComplete(BaseModel):
    handoff_message: str


def _create_agent(include_tools_in_prompt: bool) -> Agent[None, ResearchComplete]:
    cur_date = _get_now_str()

    tools = _create_base_tools()

    return Agent(
        model=create_model(),
        tools=tools,
        result_type=ResearchComplete,
        result_tool_description="Invoke this once you are completed with your research. Include a brief handoff message (1-2 sentences) how confident you are that your research uncovered a complete answer. (You do not need to provide the answer itself; all the documents you found will be returned to the user for you.)",
        result_tool_name="research_complete",
        model_settings=ModelSettings(
            temperature=0.0,
        ),
        system_prompt=_create_prompt(
            tools,
            cur_date,
            include_tools_in_prompt=include_tools_in_prompt,
        ),
    )


def _get_now_str() -> str:
    return datetime.datetime.now().strftime("%Y-%m-%d")


def _get_search_tool() -> Tool[None]:
    return create_google_search_tool()


def _create_base_tools() -> list[Tool[Any]]:
    scraper_tool = create_visit_site_tool("http://localhost:3000")
    return [
        _get_search_tool(),
        create_wiki_tool(),
        create_code_execution_tool(),
        scraper_tool,
    ]


def _create_prompt_with_default_tools(
    date_str: str,
    include_tools_in_prompt: bool,
) -> str:
    return _create_prompt(
        _create_base_tools(),
        date_str,
        include_tools_in_prompt=include_tools_in_prompt,
    )


def _create_prompt(
    tools: list[Tool[Any]],
    date_str: str,
    include_tools_in_prompt: bool,
    max_tool_calls: int = 4,
) -> str:
    return Template(
        textwrap.dedent("""\
        You are a research assistant. Perform research to accomplish the given task.

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

        ## Limitations
        You may invoke multiple tools to gather information for your task.
        However, you are limited to at most **{{ max_tool_calls }}** total tool invocations.
        After invoking **{{ max_tool_calls }}** tools, you must then invoke the 'research_complete' tool to indicate that you are done.
        Additionally, you must NOT invoke the 'research_complete' tool in the same response as other tools. It must be invoked alone.

        ## Definition of done
        Your research is complete when you have gathered sufficient information to respond to the task.
        At that point, invoke the tool 'research_complete' to indicate that you are done.
        It is not your responsibility to write a summary or report.
        Simply invoke 'research_complete' to share your findings.
        """).strip(),
    ).render(
        tools=tools,
        date_str=date_str,
        max_tool_calls=max_tool_calls,
        include_tools_in_prompt=include_tools_in_prompt,
    )
