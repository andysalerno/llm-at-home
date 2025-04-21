import os
import logging
from langchain_openai import ChatOpenAI
from pydantic import SecretStr


logger = logging.getLogger(__name__)


def create_model():
    api_key = os.environ.get("LLM_API_KEY")

    model_name = os.environ.get("MODEL_NAME")
    logger.info(f"using model: {model_name}")

    llm_base_url = os.environ.get("LLM_BASE_URL")
    logger.info(f"using llm base url: {llm_base_url}")

    if not api_key:
        raise ValueError("LLM_API_KEY environment variable not set")
    if not model_name:
        raise ValueError("MODEL_NAME environment variable not set")
    if not llm_base_url:
        raise ValueError("LLM_BASE_URL environment variable not set")

    api_key = SecretStr(api_key)

    model = ChatOpenAI(
        base_url=llm_base_url,
        model=model_name,
        api_key=api_key,
        temperature=0.0,
    )

    return model
