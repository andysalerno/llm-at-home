import json
from langchain_google_community import GoogleSearchAPIWrapper
from pydantic_ai import Tool


def create_google_search_tool(top_n: int = 8) -> Tool[None]:
    search_wrapper = GoogleSearchAPIWrapper()

    def search(query: str) -> str:
        """
        Args:
            query: The query to search for. Remember, this is a google query, so write your query as you would in Google.
        """
        results = search_wrapper.results(query, num_results=top_n)
        return json.dumps(results)

    tool = Tool(
        name="google_search",
        description="Search Google for the given query. Returns the search results (including snippet and url) in JSON format. Useful for looking up current events.",
        function=search,
        takes_ctx=False,
    )

    return tool
