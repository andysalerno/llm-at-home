import os
from pydantic_ai.models.openai import OpenAIModel
from pydantic_ai.providers.openai import OpenAIProvider
from pydantic_ai.agent import InstrumentationSettings
from pydantic_ai.models.instrumented import InstrumentedModel
import logging

logger = logging.getLogger(__name__)

instrumentation_settings = InstrumentationSettings(event_mode="logs")


def get_instrumentation_settings():
    return instrumentation_settings


def create_model():
    api_key = os.environ.get("LLM_API_KEY")

    model_name = os.environ.get("MODEL_NAME")
    logger.info(f"using model: {model_name}")

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
