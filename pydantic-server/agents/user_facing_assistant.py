from pydantic_ai import Agent, Tool
from pydantic_ai.models import Model
from pydantic_ai.settings import ModelSettings
from jinja2 import Template
from agents.tool_calling_agent import TemplatedTool
from agents.tool_calling_agent import create_tool_calling_agent
from datetime import datetime
import textwrap


def _create_researcher_tool(
    model: Model, tools: list[Tool], annotate_tools: bool = False
) -> Tool:
    def ask_researcher(task: str) -> str:
        settings = ModelSettings(temperature=0.0)
        tool_agent = create_tool_calling_agent(model, tools)
        result = tool_agent.run_sync(task, model_settings=settings)

        if annotate_tools:
            return (
                "<tool_output>"
                + result.data.answer
                + "</tool_output>\n (tool output is not visible to the user; use it to provide your response)"
            )
        else:
            return result.data.answer

    return Tool(
        description=textwrap.dedent(
            """\
            Invokes a researcher for information, and returns its response. The researcher accepts tasks and responds with answers, data, code, or information. 
            """.strip()
        ),
        function=ask_researcher,
    )


def create_user_facing_assistant(model: Model, tools: list[Tool]):
    date = datetime.now().strftime("%Y-%m-%d")
    agent = Agent(
        model,
        system_prompt=_create_prompt([], date),
        tools=[_create_researcher_tool(model, tools, annotate_tools=True)],
    )

    return agent


def _create_prompt(tools: list[TemplatedTool], date_str: str) -> str:
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

        ## On tools and assistants
        You have access to a research assistant. Whenever you need information, ask the assistant by invoking its tool and providing it the task.
        When you need to invoke the researcher, *drop the chat persona!* You only use the persona with the user.
        The tool is named: `ask_researcher`. It takes a single string argument, which is the task you want the assistant to perform.
        When you need to invoke a tool or assistant, do so immediately. Don't waste time telling the user what you are about to do, just do it.

        ## Additional context
        The current date is: {{ date_str }}

        Most importantly of all: don't be cringe!
        """).strip()
    ).render(tools=tools, date_str=date_str)
