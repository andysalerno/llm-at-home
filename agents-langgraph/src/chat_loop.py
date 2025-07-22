from langgraph.graph.state import CompiledStateGraph
from langchain_core.messages import BaseMessage, ChatMessage

from custom_react import ChatState


def run_chat_loop(graph: CompiledStateGraph):
    conversation_history: ChatState = {"messages": []}

    while True:
        user_input = input("User: ")
        if user_input.lower() in ["quit", "exit", "q"]:
            print("Goodbye!")
            break

        # user_message = {"role": "user", "content": user_input}

        conversation_history["messages"].append(
            # BaseMessage(content=user_input, role="user")
            ChatMessage(content=user_input, role="user")
        )

        for event in graph.stream(conversation_history):
            for value in event.values():
                assistant_message = value["messages"][-1]
                assert isinstance(assistant_message, BaseMessage), (
                    f"Expected BaseMessage but got {type(assistant_message)}"
                )
                conversation_history["messages"].append(assistant_message)
                assistant_message.pretty_print()
                # print("Message:", assistant_message.content)
