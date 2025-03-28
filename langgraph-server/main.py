import os
from langgraph.prebuilt import create_react_agent
from langchain_openai import ChatOpenAI
from pydantic import SecretStr


def configure_phoenix():
    from phoenix.otel import register
    from openinference.instrumentation.openai import OpenAIInstrumentor

    # configure the Phoenix tracer
    tracer_provider = register(
        auto_instrument=True,  # Auto-instrument your app based on installed OI dependencies
    )

    # register()
    # OpenAIInstrumentor().instrument()


configure_phoenix()


def create_model():
    api_key = os.environ.get("LLM_API_KEY")

    model_name = os.environ.get("MODEL_NAME")
    print(f"using model: {model_name}")

    if not api_key:
        raise ValueError("LLM_API_KEY environment variable not set")
    if not model_name:
        raise ValueError("MODEL_NAME environment variable not set")

    api_key = SecretStr(api_key)

    model = ChatOpenAI(
        base_url="https://openrouter.ai/api/v1", model=model_name, api_key=api_key
    )

    return model


def search(query: str):
    """Call to surf the web."""
    if "sf" in query.lower() or "san francisco" in query.lower():
        return "It's 60 degrees and foggy."
    return "It's 90 degrees and sunny."


agent = create_react_agent(create_model(), tools=[search])
output = agent.invoke(
    {"messages": [{"role": "user", "content": "what is the weather in sf"}]}
)
print(output)
