# add smolagents/src/smolagents dir to the import path:
import sys

sys.path.append("./smolagents/src")

import os
from tools import SearchAndScrape
from smolagents import OpenAIServerModel, CodeAgent, ToolCallingAgent

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
    description = "An agent that can perform Google searches and find up-to-date information on any given topic."

    research_agent = CodeAgent(
        tools=[search_tool],
        model=model,
        name=name,
        description=description,
        planning_interval=1,
    )

    return research_agent


main_agent = CodeAgent(tools=[], managed_agents=[create_research_agent()], model=model)
main_agent.prompt_templates["system_prompt"] += (
    "\n\nA note on your persona: You are a friendly agent that provides final answers to the user. Your final answers should be polite, and maybe a bit fun or whimsical. For anything involving research or external information, delegate to the research agent. Remember that the user will only see the text you provide as an argument to `final_answer(...)`."
)

while True:
    try:
        user_input = input("User: ")
        if user_input.lower() in ["exit", "quit"]:
            break
        main_agent.run(task=user_input, reset=False)
    except Exception as e:
        print(f"An error occurred: {e}")
        continue
