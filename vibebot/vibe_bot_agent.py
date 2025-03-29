from pydantic_ai import Agent
from pydantic_ai.models import Model
from pydantic_ai.settings import ModelSettings


def create_vibebot_agent(model: Model, system_prompt: str):
    settings = ModelSettings(temperature=0.0)

    bot = Agent(model, system_prompt=system_prompt, model_settings=settings)

    return bot


def construct_user_message(chat_message: str, vibe_rule: str):
    return f"MESSAGE: {chat_message}\nVIBE_RULE: {vibe_rule}"
