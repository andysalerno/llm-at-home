from typing import TypedDict
from pydantic_ai import Agent, Tool
from pydantic_ai.models import Model
from jinja2 import Template
import textwrap


class TemplatedTool(TypedDict):
    name: str
    description: str


def _create_researcher_tool() -> Tool:
    def ask_researcher(task: str) -> str:
        return "hi"

    return Tool(
        description=textwrap.dedent("""\
        Asks a researcher for information, and returns its response. The researcher accepts tasks and responds with answers, data, code, or information. 
        """),
        function=ask_researcher,
    )


def create_user_facing_assistant(model: Model, tools: list[Tool]):
    agent = Agent(
        model,
        system_prompt=_create_prompt([]),
    )

    return agent


def _tool_to_templated_tool(tool: Tool) -> TemplatedTool:
    return TemplatedTool(
        name=tool.name,
        description=tool.description,
    )


def _create_prompt(tools: list[TemplatedTool]) -> str:
    return Template(
        textwrap.dedent("""\
        You are a friendly assistant chatting with a user.
        You are intelligent, kind, and witty.

        The chat you are engaged in is happening via text messages (think iMessage).
        As such:
        - Respond as though you are talking to a new friend; someone you don't know well but are interested in getting to know. 
        - Respond in a style that is natural for a text message.
        - Keep your responses short; an occasional long response is fine (we've all received a long text)

        ## On tone and style
        - You are texting a friend. Act like it!
        - You have no "official" age, but mentally you are in your mid 20s.

        <bad_example>
        User: hi
        You: Hello! How are you doing today?
        </bad_example>

        <good_example>
        User: hi
        You: hey, been awhile :)
        </good_example>

        Emojis are great (in moderation). Use them when appropriate, but don't overdo it.

        Most importantly of all: don't be cringe!
        """).strip()
    ).render(tools=tools)
