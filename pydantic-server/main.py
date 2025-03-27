import os
from pydantic_ai import Agent
from pydantic_ai.models.openai import OpenAIModel
from pydantic_ai.settings import ModelSettings
from pydantic_ai.providers.openai import OpenAIProvider
from pydantic_ai.agent import InstrumentationSettings
from pydantic_ai.models.instrumented import InstrumentedModel
from pydantic_ai.common_tools.duckduckgo import duckduckgo_search_tool
from agents.tool_calling_agent import create_tool_calling_agent
from agents.user_facing_assistant import create_user_facing_assistant
from joke_example import run_example

instrumentation_settings = InstrumentationSettings(event_mode="logs")


def configure_phoenix():
    from phoenix.otel import register
    from openinference.instrumentation.openai import OpenAIInstrumentor

    register()
    OpenAIInstrumentor().instrument()


configure_phoenix()


def create_model():
    api_key = os.environ.get("LLM_API_KEY")

    model_name = os.environ.get("MODEL_NAME")
    print(f"using model: {model_name}")

    if not api_key:
        raise ValueError("LLM_API_KEY environment variable not set")
    if not model_name:
        raise ValueError("MODEL_NAME environment variable not set")

    model = OpenAIModel(
        model_name,
        provider=OpenAIProvider(
            base_url="https://openrouter.ai/api/v1",
            api_key=api_key,
        ),
    )

    return InstrumentedModel(model, instrumentation_settings)


model = create_model()

agent = create_user_facing_assistant(model, [duckduckgo_search_tool()])

agent.instrument_all(instrumentation_settings)

# run until the user says "exit":
message_history = None
usage = None
while True:
    user_input = input("You: ")
    if user_input.lower() == "exit":
        break

    settings = ModelSettings(temperature=0.15)
    result = agent.run_sync(
        user_input, message_history=message_history, model_settings=settings
    )
    message_history = result.all_messages()
    usage = usage + result.usage() if usage else result.usage()
    print(result.data)
    print(result.usage())

print(usage)
