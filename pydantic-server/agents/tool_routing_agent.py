from pydantic_ai import Agent, Tool
from pydantic_ai.models import Model
from pydantic_ai.messages import ModelMessage, ModelRequest
from pydantic_ai.settings import ModelSettings
from jinja2 import Template
from agents.tool_calling_agent import TemplatedTool
from datetime import datetime
import textwrap


def _create_inner_agent(model: Model):
    return Agent(
        model,
        system_prompt=textwrap.dedent(
            """\
            You are a friendly assistant chatting with a user.
            You are intelligent, kind, and witty.

            The chat you are engaged in is happening via text messages (think iMessage).
            As such:
            - Respond as though you are talking to a new friend; someone you don't know well but are interested in getting to know. 
            - Respond in a style that is natural for a text message.
            - Keep your responses short; an occasional long response is fine (we've all received a long text)
            """.strip()
        ),
    )


def _create_assistant_tool(model: Model, message_history: list[ModelMessage]) -> Tool:
    # pop the last message from the message history
    task = message_history.pop()

    if not isinstance(task, ModelRequest):
        raise ValueError("Task must be a ModelRequest")

    def respond_to_user() -> str:
        settings = ModelSettings(temperature=0.1)
        assistant_agent = _create_inner_agent(model)
        result = assistant_agent.run_sync(
            task.parts[-1].content,
            message_history=message_history,
            model_settings=settings,
        )

        return result.data

    return Tool(
        description=textwrap.dedent(
            """\
            Invokes a researcher for information, and returns its response. The researcher accepts tasks and responds with answers, data, code, or information. 
            """.strip()
        ),
        function=ask_researcher,
    )


def create_user_facing_router(model: Model, tools: list[Tool]):
    date = datetime.now().strftime("%Y-%m-%d")
    agent = Agent(
        model,
        system_prompt=_create_prompt([], date),
        tools=[_create_assistant_tool(model)],
    )

    return agent


def _create_prompt(tools: list[TemplatedTool], date_str: str) -> str:
    return Template(
        textwrap.dedent("""\
        You are tasked with reading through a conversation between a user and an assistant.
                        
        It is your responsibility to decide what should happen next:
        - Should the assistant respond to the user?
        - Should a tool be invoked to get some extra information for the assistant?

        ## Making the decision
        You should decide for the assistant to respond to the user if:
        - The user is engaging in small talk or otherwise chatting with the assistant, and no extra information is needed.
        - The user is asking a question that the assistant can answer without needing to invoke a tool (no extra information needed).

        You should decide for the assistant to invoke a tool if:
        - The assistant's response requries extra information that is not available in the conversation.
        - An appropriate tool is available to provide the extra information needed to answer the user's question.
        
        ## Answer format
        To signal for the assistant to respond to the user, invoke the tool called:
        `respond_to_user()`
    
        (The tool has no inputs; it merely signals to the assistant that it should respond.)

        You may also invoke the following tools:
        {%- for tool in tools %}
        - {{ tool.name }
          - {{ tool.description }}
        {%- endfor %}
                        
        ## Additional context
        The current date is: {{ date_str }}.
        """).strip()
    ).render(tools=tools, date_str=date_str)
