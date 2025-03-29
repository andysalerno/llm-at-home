import os
from pydantic_ai import Agent
from pydantic_ai.models import Model
from pydantic_ai.models.openai import OpenAIModel
from pydantic_ai.settings import ModelSettings
from pydantic_ai.providers.openai import OpenAIProvider
from pydantic_ai.agent import InstrumentationSettings
from pydantic_ai.models.instrumented import InstrumentedModel

from prompt import CachingPromptProvider
from vibebot.server import run_server


def _configure_phoenix():
    from phoenix.otel import register
    from openinference.instrumentation.openai import OpenAIInstrumentor

    register()
    OpenAIInstrumentor().instrument()


def is_telemetry_enabled():
    value = os.environ.get("ENABLE_LOCAL_TELEMETRY")
    if value is None:
        return False

    return value == "1" or value.lower() == "true"


def _create_model():
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

    if is_telemetry_enabled():
        instrumentation_settings = InstrumentationSettings(event_mode="logs")
        return InstrumentedModel(model, instrumentation_settings)
    else:
        return model


class StateManager:
    def __init__(self, model: Model, prompt_provider: CachingPromptProvider):
        self.model = model
        self.prompt_provider = prompt_provider

    def get_model(self):
        return self.model

    def get_prompt_provider(self):
        return self.prompt_provider


def main():
    if is_telemetry_enabled():
        _configure_phoenix()

    model = _create_model()

    prompt_provider = CachingPromptProvider(
        system_prompt_path="config/system_prompt.txt",
        vibe_path="config/vibe.txt",
        expiration_seconds=5,
    )

    state_manager = StateManager(model, prompt_provider)

    def _construct_user_message(chat_message: str) -> str:
        vibe_rule = state_manager.get_prompt_provider().get_vibe_rule()
        return f"MESSAGE: {chat_message}\nVIBE_RULE: {vibe_rule}"

    def _create_vibebot_agent() -> Agent:
        settings = ModelSettings(temperature=0.0)

        agent = Agent(
            state_manager.get_model(),
            system_prompt=state_manager.get_prompt_provider().get_system_prompt(),
            model_settings=settings,
        )

        if is_telemetry_enabled():
            agent.instrument_all()

        return agent

    run_server(_create_vibebot_agent, _construct_user_message)


if __name__ == "__main__":
    main()
