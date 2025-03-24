import os
from tools import SearchAndScrape
from smolagents import (
    OpenAIServerModel,
    CodeAgent,
)

from phoenix.otel import register
from openinference.instrumentation.smolagents import SmolagentsInstrumentor

register()
SmolagentsInstrumentor().instrument()

OPENROUTER_KEY = os.environ.get("LLM_API_KEY")
MODEL_NAME = os.environ.get("MODEL_NAME")

if OPENROUTER_KEY is None:
    raise ValueError("Please set the environment variable 'LLM_API_KEY'")

if MODEL_NAME is None:
    raise ValueError("Please set the environment variable 'MODEL_NAME'")

model = OpenAIServerModel(
    model_id=MODEL_NAME,
    api_key=OPENROUTER_KEY,
    api_base="https://openrouter.ai/api/v1",
)

search_tool = SearchAndScrape.SearchAndScrape(
    scraper_endpoint="http://localhost:8002/scrape",
    scores_endpoint="http://localhost:8001/scores",
    max_sites_to_scrape=7,
    max_chunks_to_return=4,
)


def create_research_agent():
    name = "research_agent"
    description = "An agent that can perform Google searches"

    research_agent = CodeAgent(
        tools=[search_tool], model=model, name=name, description=description
    )

    return research_agent


main_agent = CodeAgent(tools=[search_tool], model=model)

main_agent.run(task="how do I open a new firewall port in opensuse tumbleweed")
