import os
from smolagents import OpenAIServerModel, CodeAgent, WebSearchTool, VisitWebpageTool

from phoenix.otel import register
from openinference.instrumentation.smolagents import SmolagentsInstrumentor

register(auto_instrument=True)
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


main_agent = CodeAgent(tools=[WebSearchTool(), VisitWebpageTool()], model=model)

while True:
    try:
        user_input = input("User: ")
        if user_input.lower() in ["exit", "quit"]:
            break
        main_agent.run(task=user_input, reset=False)
    except Exception as e:
        print(f"An error occurred: {e}")
        continue
