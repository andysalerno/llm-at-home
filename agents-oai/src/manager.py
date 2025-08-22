from __future__ import annotations
import os

from agents import Runner
from agent_definitions.responding_agent import create_responding_agent
from agents.mcp import MCPServer

USE_HANDOFFS = os.getenv("USE_HANDOFFS", "true") == "true"
RESPONDING_AGENT_TEMP = float(os.getenv("RESPONDING_AGENT_TEMP", "0.2"))
RESPONDING_AGENT_TOP_P = float(os.getenv("RESPONDING_AGENT_TOP_P", "0.75"))
MAX_TURNS = int(os.getenv("MAX_TURNS", "25"))


async def run_single(input: str, input_context: list, mcp_server: MCPServer):
    responding_agent = await create_responding_agent(
        use_handoffs=USE_HANDOFFS,
        temperature=RESPONDING_AGENT_TEMP,
        top_p=RESPONDING_AGENT_TOP_P,
        researcher_mcp_server=mcp_server,
    )
    # result = Runner.run_streamed(responding_agent, input, session=session)
    result = Runner.run_streamed(responding_agent, input_context, max_turns=MAX_TURNS)

    async for event in result.stream_events():
        if event.type == "raw_response_event":
            if event.data.type == "response.output_text.delta":
                print(event.data.delta, end="", flush=True)
            elif (
                event.data.type == "response.output_item.done"
                and event.data.item.type == "function_call"
            ):
                print(f"invoking function: {event.data.item.name}", flush=True)
        elif event.type == "agent_updated_stream_event":
            print(f"agent updated: {event.new_agent.name}", flush=True)
        elif (
            event.type == "run_item_stream_event"
            and event.item.type == "message_output_item"
        ):
            print("", flush=True)
        else:
            # print(f"unknown event: {event}", flush=True)
            pass

    return result.to_input_list()
