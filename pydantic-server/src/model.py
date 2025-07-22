import logging
import os

from pydantic_ai.agent import InstrumentationSettings
from pydantic_ai.models.instrumented import InstrumentedModel
from pydantic_ai.models.openai import OpenAIModel
from pydantic_ai.providers.openai import OpenAIProvider

logger = logging.getLogger(__name__)

instrumentation_settings = InstrumentationSettings(event_mode="logs")


def get_instrumentation_settings() -> InstrumentationSettings:
    return instrumentation_settings


def get_extra_body(enable_thinking: bool = True):
    extra_body = {
        "provider": {
            "require_parameters": True,
        },
    }

    # if not enable_thinking:
    #     extra_body["chat_template_kwargs"] = {"enable_thinking": False}

    return extra_body


def create_model() -> InstrumentedModel:
    api_key = os.environ.get("LLM_API_KEY")

    model_name = os.environ.get("MODEL_NAME")
    logger.info("using model: %s", {model_name})

    base_url = os.environ.get("LLM_BASE_URL")

    if not api_key:
        raise ValueError("LLM_API_KEY environment variable not set")
    if not model_name:
        raise ValueError("MODEL_NAME environment variable not set")
    if not base_url:
        raise ValueError("LLM_BASE_URL environment variable not set")

    model = OpenAIModel(
        model_name,
        provider=OpenAIProvider(
            base_url=base_url,
            api_key=api_key,
        ),
    )

    original_model_request = model.request

    def _request(
        self: OpenAIModel,
        *args,
        **kwargs,
    ):
        # Custom request logic here
        logger.info("Making request with args: %s, kwargs: %s", args, kwargs)
        # Call the original request method
        return original_model_request(*args, **kwargs)

    model.request = _request.__get__(model, OpenAIModel)

    original_process_response = model._process_response

    def _process_response(
        self: OpenAIModel,
        response,
    ):
        # Custom processing logic here
        logger.info("Processing response: %s", response)
        # Call the original process_response method
        return original_process_response(response)

    # override the behavior of process_response:
    model._process_response = _process_response.__get__(model, OpenAIModel)

    return InstrumentedModel(model, instrumentation_settings)
