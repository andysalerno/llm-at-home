from mcp.server.fastmcp import FastMCP
from langchain_google_community import GoogleSearchAPIWrapper
import logging
import json
import asyncio

logger = logging.getLogger(__name__)
mcp = FastMCP("google-search")
search_wrapper = GoogleSearchAPIWrapper()

TOP_N = 8


@mcp.tool()
async def search(query: str) -> str:
    """
    Search Google for the given query. Returns the search results (including snippet and url) in JSON format.
    Useful for looking up current events.

    Args:
        query: The query to search for. Remember, this is a google query, so write your query as you would in Google.
    """
    logger.info("Searching for query: %s", query)
    results = search_wrapper.results(query, num_results=TOP_N)
    return json.dumps(results)


async def main():
    await mcp.run_sse_async()


if __name__ == "__main__":
    asyncio.run(main())
