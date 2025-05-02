from mcp.server.fastmcp import FastMCP
import asyncio
from google_search import google_search
from visit_url_tool import visit_url_tool


def setup_mcp(mcp: FastMCP):
    google_search.setup_mcp(mcp)
    visit_url_tool.setup_mcp(mcp)


async def serve(mcp: FastMCP):
    await mcp.run_sse_async()


if __name__ == "__main__":
    mcp = FastMCP("all-tools")
    setup_mcp(mcp)
    asyncio.run(serve(mcp))
