import os
from smolagents import (
    OpenAIServerModel,
    VisitWebpageTool,
    DuckDuckGoSearchTool,
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

search_tool = DuckDuckGoSearchTool()
search_tool.description = "Performs a google search based on your query then returns a summary of the top results with their respective urls."

# source_finder = ToolCallingAgent(
#     tools=[search_tool],
#     model=model,
#     name="source_finder_agent",
#     description="Runs web searches for you to find suitable websites to visit. Give it your query as an argument called 'task'. Since you don't have access to real-time information, use this agent to find real-time information, news, weather, current events, etc.",
# )

research_agent = CodeAgent(
    tools=[search_tool, VisitWebpageTool()],
    managed_agents=[],
    model=model,
    name="research_agent",
    description='Performs research and responds with results. Can search the web, find real-time information, and summarize results. Provide your query as an argument called "task".',
)

research_agent.prompt_templates["system_prompt"] += (
    "\nAdditional info:"
    + "\n- If you visit multiple websites, remember that any may fail and throw an error, so try/catch for each."
    + "\n- After invoking visit_webpage, print its output to capture the result, otherwise it will be lost."
)

main_agent = CodeAgent(tools=[], managed_agents=[research_agent], model=model)

main_agent.run(task="What movies are playing in Kirkland, WA this weekend?")
