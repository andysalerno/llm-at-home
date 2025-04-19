import json
import asyncio
import logging
from duckduckgo_search import DDGS

# from langchain_community.tools.ddg_search import DuckDuckGoSearchRun
from pydantic_ai.common_tools.duckduckgo import DuckDuckGoSearchTool
from langchain_community.tools.wikipedia.tool import WikipediaQueryRun
from langchain_community.utilities.wikipedia import WikipediaAPIWrapper

from chat_loop import run_chat_loop
from custom_react import create_custom_react_agent
from model import create_model

logger = logging.getLogger(__name__)


def _configure_phoenix():
    from phoenix.otel import register

    # from arize.otel import register
    from openinference.instrumentation.langchain import LangChainInstrumentor

    tracer_provider = register(
        auto_instrument=True,
    )

    LangChainInstrumentor().instrument(tracer_provider=tracer_provider)


def get_google_search_results(query: str) -> str:
    """
    Searches the web using Google and returns the top results. Results include a brief summary and the link.

    Args:
        query: The google query.
    """
    tool = DuckDuckGoSearchTool(DDGS())
    results = asyncio.run(tool(query))
    return json.dumps(results)


def main():
    _configure_phoenix()

    # agent_graph = create_react_agent(create_model(), tools=[get_weather])
    agent_graph = create_custom_react_agent(
        create_model(),
        tools=[
            get_google_search_results,
            WikipediaQueryRun(api_wrapper=WikipediaAPIWrapper()),  # type: ignore
        ],
    )

    logger.info(agent_graph.get_graph().draw_mermaid())

    run_chat_loop(agent_graph)

    pass


if __name__ == "__main__":
    main()
