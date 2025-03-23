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
)

main_agent = CodeAgent(tools=[search_tool], model=model)

main_agent.run(task="What is Elton Johhn's birthday?")
