from mcp import ClientSession
from mcp.client.sse import sse_client


async def run() -> None:
    async with (
        sse_client("http://localhost:8000/sse") as (read, write),
        ClientSession(read, write) as session,
    ):
        await session.initialize()

        tools = await session.list_tools()
        print(f"Available tools: {tools}")

        tool_result = await session.call_tool(
            "search", arguments={"query": "hello world"}
        )

        print(f"Tool result: {tool_result}")


if __name__ == "__main__":
    import asyncio

    asyncio.run(run())
