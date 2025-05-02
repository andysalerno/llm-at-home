from mcp.server.fastmcp import FastMCP
import asyncio
from google_search import google_search
from visit_url_tool import visit_url_tool
import os

PORT = int(os.getenv("MCP_SERVER_PORT", "8000"))
os.environ["FASTMCP_PORT"] = str(PORT)


def setup_mcp(mcp: FastMCP):
    google_search.setup_mcp(mcp)
    visit_url_tool.setup_mcp(mcp)
    pass


async def serve(mcp: FastMCP):
    await mcp.run_sse_async()


if __name__ == "__main__":
    mcp = FastMCP("all_tools")
    setup_mcp(mcp)
    asyncio.run(serve(mcp))
