import os
from pydantic_ai import Agent
from pydantic_ai.models import Model
from pydantic_ai.models.openai import OpenAIModel
from pydantic_ai.settings import ModelSettings
from pydantic_ai.providers.openai import OpenAIProvider
from pydantic_ai.agent import InstrumentationSettings
from pydantic_ai.models.instrumented import InstrumentedModel

from prompt import CachingPromptProvider

instrumentation_settings = InstrumentationSettings(event_mode="logs")


def configure_phoenix():
    if os.environ.get("ENABLE_LOCAL_TELEMETRY") is not None:
        from phoenix.otel import register
        from openinference.instrumentation.openai import OpenAIInstrumentor

        register()
        OpenAIInstrumentor().instrument()


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


def create_vibebot(model: Model, system_prompt: str):
    settings = ModelSettings(temperature=0.0)

    bot = Agent(model, system_prompt=system_prompt, model_settings=settings)

    return bot


def construct_user_message(chat_message: str, vibe_rule: str):
    return f"MESSAGE: {chat_message}\nVIBE_RULE: {vibe_rule}"


def main():
    model = create_model()

    prompt_provider = CachingPromptProvider(
        system_prompt_path="system_prompt.txt",
        vibe_path="vibe.txt",
        expiration_seconds=5,
    )

    agent = create_vibebot(model, prompt_provider.get_system_prompt())

    agent.instrument_all(instrumentation_settings)

    try:
        user_input = input("You: ")
    except KeyboardInterrupt:
        print("\nExiting...")
        return

    if user_input.lower() == "exit":
        return

    user_message = construct_user_message(
        chat_message=user_input,
        vibe_rule=prompt_provider.get_vibe_rule(),
    )

    result = agent.run_sync(user_message)
    print(result)


if __name__ == "__main__":
    configure_phoenix()
    main()
