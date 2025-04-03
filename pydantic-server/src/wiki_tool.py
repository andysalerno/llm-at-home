import json
from langchain_google_community import GoogleSearchAPIWrapper
from pydantic_ai.tools import Tool
from duckduckgo_search import DDGS


def create_wiki_tool() -> Tool:
    async def search_via_ddg(query: str):
        from pydantic_ai.common_tools.duckduckgo import DuckDuckGoSearchTool

        tool = DuckDuckGoSearchTool(client=DDGS())

        results = await tool("site:wikipedia.org " + query)
        return results

    def search_via_google(query: str) -> str:
        search_wrapper = GoogleSearchAPIWrapper()
        results = search_wrapper.results("site:wikipedia.org " + query, num_results=8)
        return json.dumps(results)

    # Return the summary of the first result
    return Tool(
        name="search_wikipedia",
        description="Search Wikipedia for the given query and return the results. Includes multiple document titles and body content. Great for looking up people, places, and things. Less great (but still good) for current events.",
        # function=search_via_ddg,
        function=search_via_google,
        takes_ctx=False,
    )
