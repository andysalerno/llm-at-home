import json
from typing import Optional
from langchain_google_community import GoogleSearchAPIWrapper
from pydantic_ai import Tool
import logging

logger = logging.getLogger(__name__)


def create_google_search_tool(
    top_n: int = 8,
    description: Optional[str] = None,
    name: Optional[str] = None,
) -> Tool[None]:
    search_wrapper = GoogleSearchAPIWrapper()

    name = name or "google_search"
    description = description or (
        "Search Google for the given query. Returns the search results (including snippet and url) in JSON format. "
        "Useful for looking up current events."
    )

    def search(query: str) -> str:
        """
        Args:
            query: The query to search for. Remember, this is a google query, so write your query as you would in Google.
        """
        logger.info("Searching for query: %s", query)
        results = search_wrapper.results(query, num_results=top_n)
        return json.dumps(results)

    tool = Tool(
        name=name,
        description=description,
        function=search,
        takes_ctx=False,
    )

    return tool
