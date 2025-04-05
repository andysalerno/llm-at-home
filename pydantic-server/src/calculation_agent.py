import textwrap
import datetime
import json
from typing import Any
from jinja2 import Template
from pydantic import BaseModel
from pydantic_ai import Agent, RunContext
from pydantic_ai.tools import Tool
from pydantic_ai.settings import ModelSettings
from code_execution_tool import create_code_execution_tool
from model import create_model
from state import State
from pydantic_ai.messages import (
    ModelMessage,
    ModelRequest,
    ToolReturnPart,
)


def calculation_agent_tool() -> Tool:
    return Tool(
        function=_run_calculation_agent,
        name="perform_calculations",
        takes_ctx=True,
    )


async def _run_calculation_agent(ctx: RunContext[State], task: str) -> str:
    """
    Gives a task to a research agent and returns the final result of the research.
    Tasks are in natural language and can be anything from "What is the capital of France?" to "Write a Python script that calculates the Fibonacci sequence."
    Tasks can be simple or complex.
    The research agent will return a report with the results of the research.

    Args:
        task: The task to be performed by the calculation agent.
    """
    agent = _create_agent()

    system_prompt = _create_prompt_with_default_tools(_get_now_str())
    state: State = ctx.deps.with_system_prompt_replaced(system_prompt)

    result = await agent.run(task, message_history=state.message_history)

    tool_outputs = _extract_tool_return_parts(result.new_messages())

    return json.dumps(tool_outputs, indent=2)


def _extract_tool_return_parts(
    message_history: list[ModelMessage],
) -> list[dict[str, str]]:
    """
    Extracts the ToolReturnParts from the message history.
    """
    return [
        {"tool_name": part.tool_name, "result": part.content}
        for msg in message_history
        if isinstance(msg, ModelRequest)
        for part in msg.parts
        if isinstance(part, ToolReturnPart)
    ]


class CalculationsComplete(BaseModel):
    pass


def _create_agent():
    cur_date = _get_now_str()

    tools = _create_base_tools()

    agent = Agent(
        model=create_model(),
        tools=tools,
        result_type=CalculationsComplete,
        result_tool_description="Invoke this once you are completed with your calculations.",
        result_tool_name="calculations_complete",
        model_settings=ModelSettings(
            temperature=0.0,
        ),
        system_prompt=_create_prompt(
            tools,
            cur_date,
        ),
    )

    return agent


def _get_now_str() -> str:
    return datetime.datetime.now().strftime("%Y-%m-%d")


def _create_base_tools() -> list[Tool[Any]]:
    return [
        create_code_execution_tool(),
    ]


def _create_prompt_with_default_tools(date_str: str) -> str:
    return _create_prompt(_create_base_tools(), date_str)


def _create_prompt(
    tools: list[Tool[Any]], date_str: str, max_tool_calls: int = 4
) -> str:
    return Template(
        textwrap.dedent("""\
        You are a research assistant. Perform research to accomplish the given task.

        Since your internal knowledge is limited, you may invoke the following tools to get more information (including up-to-date information):
        {%- for tool in tools %}
        - {{ tool.name }}
          - {{ tool.description }}
        {%- endfor %}
                        
        ## Additional context
        The current date is: {{ date_str }}.

        ## Limitations
        You may invoke multiple tools to gather information for your task.
        However, you are limited to at most **{{ max_tool_calls }}** total tool invocations.
        After invoking **{{ max_tool_calls }}** tools, you must then invoke the 'research_complete' tool to indicate that you are done.

        ## Definition of done
        Your research is complete when you have gathered sufficient information to respond to the task.
        At that point, invoke the tool 'research_complete' to indicate that you are done. 
        It is not your responsibility to write a summary or report.
        Simply invoke 'research_complete' to share your findings.
        """).strip()
    ).render(tools=tools, date_str=date_str, max_tool_calls=max_tool_calls)