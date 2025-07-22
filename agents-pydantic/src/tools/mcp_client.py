import logging

from mcp import ClientSession
from mcp.client.sse import sse_client

logger = logging.getLogger(__name__)


async def run() -> None:
    async with (
        sse_client("http://localhost:8000/sse") as (read, write),
        ClientSession(read, write) as session,
    ):
        await session.initialize()

        tools = await session.list_tools()
        logger.info(f"Available tools: {tools}")


if __name__ == "__main__":
    import asyncio

    asyncio.run(run())
