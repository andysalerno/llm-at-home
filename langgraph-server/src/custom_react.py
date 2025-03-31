from typing import Callable
from langgraph.prebuilt.tool_node import ToolNode
from langchain_core.language_models import BaseChatModel
from typing import Annotated
from typing_extensions import TypedDict
from langgraph.graph import StateGraph, START, END
from langgraph.graph.message import add_messages
from langgraph.graph.state import CompiledStateGraph
from langchain_core.messages import BaseMessage


class ChatState(TypedDict):
    messages: Annotated[list[BaseMessage], add_messages]


def create_simple_chat_agent(model: BaseChatModel) -> CompiledStateGraph:
    def _simple_chat(state: ChatState) -> ChatState:
        return {"messages": [model.invoke(state["messages"])]}

    graph_builder = StateGraph(ChatState)
    graph_builder.add_edge(START, "simple_chat")
    graph_builder.add_node("simple_chat", _simple_chat)
    graph_builder.add_edge("simple_chat", END)

    graph = graph_builder.compile()

    return graph


def create_custom_react_agent(model: BaseChatModel, tools: list[Callable]):
    model_with_tools = model.bind_tools(tools)

    def _simple_chat(state: ChatState) -> ChatState:
        return {"messages": [model_with_tools.invoke(state["messages"])]}

    graph_builder = StateGraph(ChatState)

    graph_builder = StateGraph(ChatState)
    graph_builder.add_edge(START, "simple_chat")
    graph_builder.add_node("simple_chat", _simple_chat)

    graph_builder.add_node("handle_tools", ToolNode(tools))
    graph_builder.add_conditional_edges("simple_chat", maybe_route_tool)
    graph_builder.add_edge("handle_tools", "simple_chat")

    # tool_node = ToolNode(tools)
    # tool_classes = list(tool_node.tools_by_name.values())

    return graph_builder.compile()


def maybe_route_tool(state: ChatState):
    state["messages"]
    if messages := state.get("messages", []):
        ai_message = messages[-1]
    else:
        raise ValueError(f"No messages found in input state to tool_edge: {state}")
    if hasattr(ai_message, "tool_calls") and len(ai_message.tool_calls) > 0:  # type: ignore
        return "handle_tools"
    return END
