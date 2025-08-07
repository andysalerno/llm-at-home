import datetime
import logging
import textwrap

from jinja2 import Template
from pydantic import BaseModel
from pydantic_ai import Agent, RunContext
from pydantic_ai.mcp import MCPServerSSE
from pydantic_ai.result import ToolOutput
from pydantic_ai.settings import ModelSettings
from pydantic_ai.tools import Tool

from model import create_model, get_extra_body
from state import State

logger = logging.getLogger(__name__)


def coding_agent_tool(
    tool_name: str | None = None,
    tool_description: str | None = None,
    agent_temp: float = 0.0,
) -> Tool:
    async def _run(ctx: RunContext[State], task: str) -> str:
        """
        Args:
            task: The task to be performed, in natural language.

        Returns:
            The result of the task, in natural language.
        """
        return await _run_coding_agent(ctx, task, agent_temp)

    if tool_name is None:
        tool_name = "ask_coding_assistant"

    if tool_description is None:
        tool_description = (
            "Handoff a task to a coding agent and returns its final result."
            ' Tasks are in natural language and can be anything from "How do I reverse a string in Rust?" to "Create a full-stack web application using asp.net core, mongodb, and react, packaged with a Dockerfile Docker compose file."'
            " The coding agent is also fantastic at math and can perform simple or complex calculations."
            " Tasks can be simple or complex."
            " The coding agent will return a report with the results of the task."
        )

    return Tool(
        function=_run,
        name=tool_name,
        description=tool_description,
        takes_ctx=True,
    )


async def _run_coding_agent(
    ctx: RunContext[State],
    task: str,
    agent_temp: float = 0.0,
) -> str:
    agent = _create_agent(agent_temp)

    system_prompt = _create_prompt_with_default_tools(
        _get_now_str(),
    )
    state: State = ctx.deps.with_system_prompt_replaced(system_prompt)

    async with agent:
        result = await agent.run(task, message_history=state.message_history)

    logger.info(f"new messages: {result.new_messages()}")

    return result.output.final_answer


class TaskComplete(BaseModel):
    final_answer: str


tool_output_definition = ToolOutput(
    type_=TaskComplete,
    name="final_answer",
    description="Invoke this once you are completed with your task. MUST be invoked alone, not allowed to invoke this in parallel with other tools. Include a brief handoff message (1-2 sentences) how confident you are that your research uncovered a complete answer.",
    strict=True,
)


def _create_agent(
    temp: float = 0.0,
) -> Agent[None, TaskComplete]:
    cur_date = _get_now_str()

    mcp_server = MCPServerSSE(url="http://localhost:8002/sse")

    return Agent(
        model=create_model(),
        tools=[],
        toolsets=[mcp_server],
        output_type=tool_output_definition,
        model_settings=ModelSettings(
            temperature=temp,
            parallel_tool_calls=False,
            extra_body=get_extra_body(),
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
        You are a coding assistant. You have been tasked with helping the user with a math, logic, or coding task.

        You should:
        - call whatever tools are necessary to perform the task.
        - after observing the output, invoke `final_answer` to mark the task as complete.

        Additional rules:
        - NEVER invoke `final_answer` at the same time as other tools. You must first observe the output of any tool calls, THEN invoke `final_answer`.
        - Your response output MUST be a json array of one or more tool calls - even if you wish to invoke just one tool, emit a json array with that single tool call:

        ## Additional context
        The current date is: {{ date_str }}.
        """).strip(),
    ).render(
        date_str=date_str,
    )
