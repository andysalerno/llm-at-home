from __future__ import annotations

from agents import Runner
from agent_definitions.responding_agent import create_responding_agent
from agents.mcp import MCPServerStreamableHttp


async def run_single(input: str):
    async with MCPServerStreamableHttp(
        params={"url": "http://localhost:8002/mcp"},
        cache_tools_list=True,
    ) as mcp_server:
        responding_agent = await create_responding_agent(
            use_handoffs=True, researcher_mcp_server=mcp_server
        )
        result = Runner.run_streamed(responding_agent, input)

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
                print(f"unknown event: {event}", flush=True)

        print("", flush=True)
