from mcp.server.fastmcp import FastMCP
from langchain_google_community import GoogleSearchAPIWrapper
import logging
import json


def setup_mcp(mcp: FastMCP):
    TOP_N = 8

    logger = logging.getLogger(__name__)
    search_wrapper = GoogleSearchAPIWrapper()

    @mcp.tool()
    async def search_web(query: str) -> str:
        """
        Search Google for the given query. Returns the search results (including snippet and url) in JSON format.
        Useful for looking up: current events, up-to-date information, and general knowledge across the web.

        Args:
            query: The query to search for. Remember, this is a google query, so write your query as you would in Google.
        """
        logger.info("Searching for query: %s", query)
        results = search_wrapper.results(query, num_results=TOP_N)
        return json.dumps(results)


async def _serve(mcp: FastMCP):
    await mcp.run_sse_async()


if __name__ == "__main__":
    import asyncio

    mcp = FastMCP("google_search")
    setup_mcp(mcp)
    asyncio.run(_serve(mcp))
