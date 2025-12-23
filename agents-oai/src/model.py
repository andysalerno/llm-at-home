from agents import Model
from agents.extensions.models.litellm_model import LitellmModel

MODEL: Model | None = None


def initialize_model(model: str, api_key: str, api_base: str) -> None:
    """Initialize the global model instance."""
    global MODEL
    if MODEL is not None:
        raise ValueError("Model has already been initialized.")

    MODEL = LitellmModel(model=model, api_key=api_key, base_url=api_base)


def get_model() -> Model:
    assert MODEL is not None
    return MODEL
