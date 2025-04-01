from pydantic_ai.tools import Tool
from duckduckgo_search import DDGS
import asyncio


def create_wiki_tool() -> Tool:
    def search_via_ddg(query: str):
        from pydantic_ai.common_tools.duckduckgo import DuckDuckGoSearchTool

        tool = DuckDuckGoSearchTool(client=DDGS())

        results = asyncio.run(tool("site:wikipedia.org " + query))
        return results

    # Return the summary of the first result
    return Tool(
        name="search_wikipedia",
        description="Search Wikipedia for the given query and return the results. Includes multiple document titles and body content. Great for looking up people, places, and things. Less great (but still good) for current events.",
        function=search_via_ddg,
    )
