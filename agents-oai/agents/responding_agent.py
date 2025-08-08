import datetime
import textwrap

from agents import Agent, ModelSettings
from jinja2 import Template
from model import get_model
from agents.tool import Tool
from agents.research_agent import research_agent_tool


def create_responding_assistant(
    temperature: float = 0.2,
    extra_tools: list[Tool] | None = None,
) -> Agent:
    if extra_tools is None:
        extra_tools = []
    cur_date = datetime.datetime.now().strftime("%Y-%m-%d")

    tools = [
        research_agent_tool(),
        # research_agent_tool(include_tools_in_prompt, agent_temp=0.2),
        # coding_agent_tool(agent_temp=0.1),
        # coding_agent_tool(
        #     tool_name="calculator",
        #     tool_description="Performs a calculation, described in natural language, such as: '2 + 2', 'days between 2023-01-08 and 2024-02-12', '15 pounds times 23 kilograms', etc.",
        #     agent_temp=0.1,
        # ),
        *extra_tools,
    ]

    agent = Agent(
        name="RespondingAgent",
        tools=tools,
        instructions=_create_prompt(cur_date),
        model=get_model(),
        model_settings=ModelSettings(
            temperature=temperature,
            # extra_body=get_extra_body(),
        ),
    )

    return agent


def _create_prompt(
    date_str: str,
) -> str:
    return Template(
        textwrap.dedent("""\
        You are a helpful assistant. Help the user as best you can.

        Since your internal knowledge is limited, you may invoke tools to get more information (including up-to-date information).

        ## Additional context
        The current date is: {{ date_str }}.

        ## Additional rules
        - Always prefer to use the research assistant over your own knowledge. Even when you think you know the answer, it is better to use the research assistant to get the most accurate and up-to-date information, and to discover sources to provide to the user.
        - If you still cannot find a relevant result, even after invoking the research assistant, tell the user you do not know, or invoke the researcher again with a reformulated task.
        - If you need to do any kind of calculation, delegate to the coding assistant; it is better at math than you are!
        - The research assistant may provide more information than necessary to handle the user's question. In that case, provide whatever extra context or information that you think might be useful to the user.
        - Do not mention your tools/assistants/researchers to the user; they are transparent to the user.
        - You are free to invoke tools/assistants/researchers as many times as you need to construct a complete answer. In fact, you are encouraged to break the task into smaller sub-tasks and invoke the tools/assistants/researchers for each of them.
        - Do not hallucinate! Any factual information you provide must be based on findings from the research assistant.
        - When possible, cite your sources via markdown links.
        """).strip(),
    ).render(
        date_str=date_str,
    )
