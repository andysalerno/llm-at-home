import json
import asyncio
import logging
from duckduckgo_search import DDGS

# from langchain_community.tools.ddg_search import DuckDuckGoSearchRun
from pydantic_ai.common_tools.duckduckgo import DuckDuckGoSearchTool
from langchain_community.tools.wikipedia.tool import WikipediaQueryRun
from langchain_community.utilities.wikipedia import WikipediaAPIWrapper

from agents.responding_assistant import responding_agent_graph
from chat_loop import run_chat_loop
from custom_react import create_custom_react_agent
from model import create_model
from langgraph.graph import START, END
from langchain.tools import Tool
from pydantic_ai import Tool as PydanticTool

from tools.google_search_tool import create_google_search_tool
from tools.wiki_tool import create_wiki_tool

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


def pydantic_to_langchain(tool: PydanticTool) -> Tool:
    """
    Converts a Pydantic tool to a LangChain tool.
    """
    return Tool(
        name=tool.name,
        description=tool.description,
        func=tool.function,
        args_schema=tool._base_parameters_json_schema,
    )


def main():
    logging.basicConfig(level=logging.INFO)
    _configure_phoenix()

    # agent_graph = create_react_agent(create_model(), tools=[get_weather])
    (agent_graph, input_node_name, output_node_name) = responding_agent_graph(
        create_model(),
        tools=[
            pydantic_to_langchain(create_wiki_tool()),
            pydantic_to_langchain(create_google_search_tool()),
        ],
    )

    agent_graph.add_edge(START, input_node_name)
    agent_graph.add_edge(output_node_name, END)

    agent_graph = agent_graph.compile()

    logger.info(agent_graph.get_graph().draw_mermaid())
    logger.info("\n" + agent_graph.get_graph().draw_ascii())

    run_chat_loop(agent_graph)

    pass


if __name__ == "__main__":
    main()
