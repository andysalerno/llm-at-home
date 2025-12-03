from __future__ import annotations

import logging
from typing import TYPE_CHECKING

from agents import Runner
from agents.stream_events import (
    AgentUpdatedStreamEvent,
    RawResponsesStreamEvent,
)
from openai.types.responses import ResponseOutputItemDoneEvent, ResponseTextDeltaEvent

from agent_definitions.responding_agent import create_responding_agent
from config import config
from output import Output

if TYPE_CHECKING:
    from agents.items import TResponseInputItem
    from agents.mcp import MCPServer

logger = logging.getLogger(__name__)


async def run_single(
    input: str,
    input_context: list[TResponseInputItem],
    mcp_server: MCPServer,
    output: Output,
) -> list[TResponseInputItem]:
    responding_agent = await create_responding_agent(
        use_handoffs=config.USE_HANDOFFS,
        temperature=config.RESPONDING_AGENT_TEMP,
        top_p=config.RESPONDING_AGENT_TOP_P,
        mcp_server=mcp_server,
    )
    result = Runner.run_streamed(
        responding_agent,
        input_context,
        max_turns=config.MAX_TURNS,
    )

    logger = output.logger("chat_loop")

    async for event in result.stream_events():
        if isinstance(event, RawResponsesStreamEvent):
            if isinstance(event.data, ResponseTextDeltaEvent):
                output.streaming_message(event.data.delta)
            elif (
                isinstance(event.data, ResponseOutputItemDoneEvent)
                and event.data.item.type == "function_call"
            ):
                logger.info(
                    f"\n[Invoking function: {event.data.item.name}({event.data.item.arguments})]",
                )
        elif isinstance(event, AgentUpdatedStreamEvent):
            logger.info(f"\n[Switched to agent: {event.new_agent.name}]")

    return result.to_input_list()
