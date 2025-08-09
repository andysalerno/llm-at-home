from __future__ import annotations

import asyncio
import os

from model import initialize_model
from manager import run_single
from phoenix.otel import register

# configure the Phoenix tracer
tracer_provider = register(
    project_name="agents",  # Default is 'default'
    auto_instrument=True,  # Auto-instrument your app based on installed dependencies
)


async def main():
    query = input("input: ")

    # await ResearchManager().run(query)
    output = await run_single(query)
    print("Final output:", output)


if __name__ == "__main__":
    # First try to get model/api key from args
    import argparse

    parser = argparse.ArgumentParser()
    parser.add_argument("--model", type=str, required=False)
    parser.add_argument("--api-key", type=str, required=False)
    parser.add_argument("--api-url", type=str, required=False)
    args = parser.parse_args()

    model = args.model
    if not model:
        model = os.getenv("MODEL_NAME")
    if not model:
        model = input("Enter a model name for Litellm: ")

    api_url = args.api_url
    if not api_url:
        api_url = os.getenv("MODEL_BASE_URI")
    if not api_url:
        api_url = input("Enter an API base URL for Litellm: ")

    api_key = args.api_key
    if not api_key:
        api_key = os.getenv("MODEL_API_KEY")
    if not api_key and "localhost" not in api_url:
        api_key = input("Enter an API key for Litellm: ")
    if not api_key:
        api_key = "empty"

    initialize_model(model, api_key, api_url)

    asyncio.run(main())
