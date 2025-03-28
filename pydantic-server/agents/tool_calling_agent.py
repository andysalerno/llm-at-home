from typing import TypedDict
from datetime import datetime
from pydantic import BaseModel
from pydantic_ai import Agent, Tool
from pydantic_ai.models import Model
from jinja2 import Template
import textwrap


class TemplatedTool(TypedDict):
    name: str
    description: str


class FinalAnswer(BaseModel):
    answer: str


def _create_final_answer_tool() -> Tool:
    def final_answer(answer: str):
        return answer

    return Tool(
        name="final_answer",
        description="To provide the final answer to the task. You MUST use this tool when your research is complete and you are ready to present your final answer.",
        function=final_answer,
    )


def create_tool_calling_agent(model: Model, tools: list[Tool]):
    final_answer_tool = TemplatedTool(
        name="final_answer",
        description="To provide the final answer to the task. You MUST use this tool to return your final answer.",
    )

    templated_tools = [_tool_to_templated_tool(tool) for tool in tools]
    templated_tools.append(final_answer_tool)

    # all_tools = tools + [_create_final_answer_tool()]
    all_tools = tools

    current_date = datetime.now().strftime("%Y-%m-%d")

    agent = Agent(
        model,
        system_prompt=_create_prompt(all_tools, current_date),
        tools=all_tools,
        result_tool_name="final_answer",
        result_tool_description="Invoke this when you are ready to provide the final answer to the task. You MUST use this tool to return your final answer.",
        result_type=FinalAnswer,
    )

    return agent


def _tool_to_templated_tool(tool: Tool) -> TemplatedTool:
    return TemplatedTool(
        name=tool.name,
        description=tool.description,
    )


def _create_prompt(tools: list[Tool], current_date: str) -> str:
    return Template(
        textwrap.dedent("""\
                        You are an expert research assistant.
                        You will be given a task by the user. Use your available tools to complete the task to the best of your abilities.

                        In particular, you have access to a search tool (think Google). Use it to find up-to-date information on the web and perform research.
                        The tool allows searching by domain name, so you might query e.x. "site:wikipedia.org dog breeds" 

                        ## Available Tools
                        {%- for tool in tools %}
                        - {{ tool.name }}: {{ tool.description }}
                        {%- endfor %}

                        ## Additional context
                        The current date is: {{ current_date }}

                        ## Additional instructions
                        - You may invoke tools multiple times. For instance, if you invoke the search tool, but the results are not sufficient, you may invoke it again.
                        - Invoke whatever tools you want as many times as you need, just be sure to invoke the final_answer tool when you are ready to provide the final answer.
                        """).strip()
    ).render(tools=tools, current_date=current_date)
