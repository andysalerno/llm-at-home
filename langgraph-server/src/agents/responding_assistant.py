import datetime
import textwrap
from typing import Any, Callable, Literal, Tuple, TypedDict
from pydantic_ai import Tool
from langgraph.graph.message import add_messages

from jinja2 import Template
from langgraph.graph import StateGraph
from langchain_core.language_models import BaseChatModel
from langchain_core.messages import SystemMessage

# from agents.research_agent import research_agent_tool
from model import create_model
from state import ChatState
from langchain_community.tools.wikipedia.tool import (
    WikipediaQueryRun,
    WikipediaQueryInput,
)
from langchain.tools import BaseTool


def next_llm_response_node(
    model: BaseChatModel, tools: list[BaseTool]
) -> Callable[[ChatState], ChatState]:
    model_with_tools = model.bind_tools(tools)

    def _get_response(state: ChatState) -> ChatState:
        print("running next_llm_response_node for state: ", state)
        return {"messages": [model_with_tools.invoke(state["messages"])]}

    return _get_response


def remove_system_messages_node() -> Callable[[ChatState], ChatState]:
    def _remove_system_prompts(state: ChatState) -> ChatState:
        next_messages = [
            msg for msg in state["messages"] if not isinstance(msg, SystemMessage)
        ]
        return ChatState(messages=next_messages)

    return _remove_system_prompts


def add_system_message_node(
    system_message: str, location: Literal["start", "end"] = "start"
) -> Callable[[ChatState], ChatState]:
    def _add_system_message(state: ChatState) -> ChatState:
        next_messages = []
        if location == "start":
            print("adding system message to beginning of messages")
            # next_messages = [SystemMessage(content=system_message)] + state["messages"]
            next_messages = add_messages(
                [SystemMessage(content=system_message)], state["messages"]
            )
        elif location == "end":
            # next_messages = state["messages"] + [SystemMessage(content=system_message)]
            next_messages = add_messages(
                state["messages"], [SystemMessage(content=system_message)]
            )
        else:
            raise ValueError(f"Invalid location: {location}. Must be 'start' or 'end'.")

        print("returning messages:", next_messages)

        return ChatState(messages=next_messages)

    return _add_system_message


def responding_agent_graph(
    model: BaseChatModel, tools: list[BaseTool]
) -> Tuple[StateGraph, str, str]:
    graph_builder = StateGraph(ChatState)

    LLM_RESPONSE_NODE_NAME = "next_llm_response"
    REMOVE_SYSTEM_MESSAGES_NODE_NAME = "remove_system_messages"
    ADD_SYSTEM_MESSAGE_NODE_NAME = "add_system_message"
    START = REMOVE_SYSTEM_MESSAGES_NODE_NAME
    END = LLM_RESPONSE_NODE_NAME

    graph_builder.add_node(
        REMOVE_SYSTEM_MESSAGES_NODE_NAME, remove_system_messages_node()
    )
    graph_builder.add_node(
        ADD_SYSTEM_MESSAGE_NODE_NAME,
        add_system_message_node(
            _create_prompt(tools, date_str="2023-10-01", include_tools_in_prompt=True),
            location="start",
        ),
    )
    graph_builder.add_node(LLM_RESPONSE_NODE_NAME, next_llm_response_node(model, tools))

    graph_builder.add_edge(
        REMOVE_SYSTEM_MESSAGES_NODE_NAME, ADD_SYSTEM_MESSAGE_NODE_NAME
    )
    graph_builder.add_edge(ADD_SYSTEM_MESSAGE_NODE_NAME, LLM_RESPONSE_NODE_NAME)

    return (graph_builder, START, END)


# def create_responding_assistant(
#     temperature: float = 0.2,
#     extra_tools: list[Callable] | None = None,
#     include_tools_in_prompt: bool = True,
# ) -> Agent[State, str]:
#     if extra_tools is None:
#         extra_tools = []
#     instrumentation_settings = get_instrumentation_settings()
#     cur_date = datetime.datetime.now().strftime("%Y-%m-%d")

#     tools = [research_agent_tool(include_tools_in_prompt, agent_temp=0.1), *extra_tools]

#     agent = Agent(
#         model=create_model(),
#         deps_type=State,
#         tools=tools,
#         model_settings=ModelSettings(
#             temperature=temperature,
#         ),
#         system_prompt=_create_prompt(tools, cur_date, include_tools_in_prompt),
#     )

#     agent.instrument_all(instrumentation_settings)

#     return agent


def _create_prompt(
    tools: list[BaseTool],
    date_str: str,
    include_tools_in_prompt: bool,
) -> str:
    return Template(
        textwrap.dedent("""\
        You are a helpful assistant. Help the user as best you can.

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

        ## Additional rules
        - Always prefer to use the researcher over your own knowledge. Even when you think you know the answer, it is better to use the researcher tool to get the most accurate and up-to-date information, and to discover sources to provide to the user.
        - If you still cannot find a relevant result, even after invoking the researcher, tell the user you do not know.
        - If you need to do any kind of calculation, delegate to the researcher; it is better at math than you are!
        - The research assistant may provide more information than necessary to handle the user's question. In that case, provide whatever extra context or information that you think might be useful to the user.
        - You must NOT include tool calls alongside messages to the user. Your response must be EITHER tool invocations OR a message to the user, but not both.
        """).strip(),
    ).render(
        tools=tools,
        date_str=date_str,
        include_tools_in_prompt=include_tools_in_prompt,
    )
