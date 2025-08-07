from __future__ import annotations

import asyncio

from agents import Agent, Runner, function_tool, set_tracing_disabled
from agents.extensions.models.litellm_model import LitellmModel
from model import initialize_model
from manager import ResearchManager
from phoenix.otel import register

# configure the Phoenix tracer
tracer_provider = register(
    project_name="agents",  # Default is 'default'
    auto_instrument=True,  # Auto-instrument your app based on installed dependencies
)


async def main():
    query = input("What would you like to research? ")

    await ResearchManager().run(query)


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
        model = input("Enter a model name for Litellm: ")

    api_key = args.api_key
    if not api_key:
        api_key = input("Enter an API key for Litellm: ")

    api_url = args.api_url
    if not api_url:
        api_url = input("Enter an API base URL for Litellm: ")

    initialize_model(model, api_key, api_url)

    asyncio.run(main())
