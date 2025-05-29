from mcp.server.fastmcp import FastMCP
from langchain_google_community import GoogleSearchAPIWrapper
import logging
import json


def setup_mcp(mcp: FastMCP):
    TOP_N = 8

    logger = logging.getLogger(__name__)
    search_wrapper = GoogleSearchAPIWrapper()

    @mcp.tool()
    async def search_wikipedia(query: str) -> str:
        """
        "Search Wikipedia for the given query and return the results. Includes multiple document titles and body content. "
        "Great for looking up people, places, and things. Less great (but still good) for current events."
        """
        query = f"site:wikipedia.org {query}"
        logger.info("Searching wikipedia for query: %s", query)
        results = search_wrapper.results(query, num_results=TOP_N)
        return json.dumps(results)


async def _serve(mcp: FastMCP):
    await mcp.run_sse_async()


if __name__ == "__main__":
    import asyncio

    mcp = FastMCP("search_wikipedia")
    setup_mcp(mcp)
    asyncio.run(_serve(mcp))
