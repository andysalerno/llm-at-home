import datetime
import textwrap

from agents import Agent, ModelSettings
from jinja2 import Template
from model import get_model
from agents.tool import Tool
from agent_definitions.research_agent import create_research_agent, research_agent_tool


async def create_responding_agent(
    temperature: float = 0.2,
    extra_tools: list[Tool] | None = None,
    use_handoffs: bool = True,
) -> Agent:
    if extra_tools is None:
        extra_tools = []
    cur_date = datetime.datetime.now().strftime("%Y-%m-%d")

    if use_handoffs:
        tools = extra_tools
        handoffs = [await create_research_agent(temperature)]
    else:
        tools = [await research_agent_tool(), *extra_tools]
        handoffs = []

    handoffs = [await create_research_agent(temperature)]

    agent = Agent(
        name="RespondingAgent",
        tools=tools,
        handoffs=handoffs,  # type: ignore
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
