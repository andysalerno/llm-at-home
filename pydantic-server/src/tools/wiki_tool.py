import json
from typing import Literal, Optional
from langchain_google_community import GoogleSearchAPIWrapper
from pydantic_ai.tools import Tool
from duckduckgo_search import DDGS
import logging

logger = logging.getLogger(__name__)


def create_wiki_tool(
    name: Optional[str] = None,
    description: Optional[str] = None,
    provider: Literal["google", "duckduckgo"] = "google",
) -> Tool:
    name = name or "search_wikipedia"
    description = description or (
        "Search Wikipedia for the given query and return the results. Includes multiple document titles and body content. "
        "Great for looking up people, places, and things. Less great (but still good) for current events."
    )

    async def search_via_ddg(query: str):
        from pydantic_ai.common_tools.duckduckgo import DuckDuckGoSearchTool

        tool = DuckDuckGoSearchTool(client=DDGS())

        logger.info("Searching wikipedia (DDG) for query: %s", query)
        results = await tool("site:wikipedia.org " + query)
        return results

    def search_via_google(query: str) -> str:
        logger.info("Searching wikipedia (Google) for query: %s", query)
        search_wrapper = GoogleSearchAPIWrapper()
        results = search_wrapper.results("site:wikipedia.org " + query, num_results=8)
        return json.dumps(results)

    search_fn = search_via_ddg if provider == "duckduckgo" else search_via_google

    # Return the summary of the first result
    return Tool(
        name=name,
        description=description,
        function=search_fn,
        takes_ctx=False,
    )
