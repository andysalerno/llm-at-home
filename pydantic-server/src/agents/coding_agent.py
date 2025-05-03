import datetime
import json
import logging
import textwrap

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
from pydantic_ai.result import ToolOutput
from pydantic_ai.settings import ModelSettings
from pydantic_ai.tools import Tool

from model import create_model
from state import State

logger = logging.getLogger(__name__)


def coding_agent_tool(agent_temp: float = 0.0) -> Tool:
    async def _run(ctx: RunContext[State], task: str) -> str:
        return await _run_coding_agent(ctx, task, agent_temp)

    description = (
        "Handoff a task to a coding agent and returns its final result."
        ' Tasks are in natural language and can be anything from "How do I reverse a string in Rust?" to "Create a full-stack web application using asp.net core, mongodb, and react, packaged with a Dockerfile Docker compose file."'
        " Tasks can be simple or complex."
        " The coding agent will return a report with the results of the task."
    )

    return Tool(
        function=_run,
        name="coding_assistant_handoff",
        description=description,
        takes_ctx=True,
    )


async def _run_coding_agent(
    ctx: RunContext[State],
    task: str,
    agent_temp: float = 0.0,
) -> str:
    """
    Args:
        task: The task to be performed by the coding agent. May be a simple query or complex task.
    """
    agent = _create_agent(agent_temp)

    system_prompt = _create_prompt_with_default_tools(
        _get_now_str(),
    )
    state: State = ctx.deps.with_system_prompt_replaced(system_prompt)

    async with agent.run_mcp_servers():
        result = await agent.run(task, message_history=state.message_history)

    logger.info(f"new messages: {result.new_messages()}")

    return result.output


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


class TaskComplete(BaseModel):
    handoff_message: str


tool_output_definition = ToolOutput(
    type_=TaskComplete,
    name="task_complete",
    description="Invoke this once you are completed with your task. Include a brief handoff message (1-2 sentences) how confident you are that your research uncovered a complete answer. (You do not need to provide the answer itself; all the documents you found will be returned to the user for you.)",
    strict=True,
)


def _create_agent(
    temp: float = 0.0,
) -> Agent[None]:
    cur_date = _get_now_str()

    return Agent(
        model=create_model(),
        tools=[],
        # output_type=tool_output_definition,
        model_settings=ModelSettings(
            temperature=temp,
        ),
        system_prompt=_create_prompt(
            cur_date,
        ),
    )


def _get_now_str() -> str:
    return datetime.datetime.now().strftime("%Y-%m-%d")


def _create_prompt_with_default_tools(
    date_str: str,
) -> str:
    return _create_prompt(
        date_str,
    )


def _create_prompt(
    date_str: str,
) -> str:
    return Template(
        textwrap.dedent("""\
        You are a coding assistant. You have been tasked with helping the user with a coding task.

        ## Additional context
        The current date is: {{ date_str }}.
        """).strip(),
    ).render(
        date_str=date_str,
    )
