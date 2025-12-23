from __future__ import annotations

import asyncio
import logging

from agents.mcp import MCPServerStreamableHttp
from phoenix.otel import register

from chat_loop import run_single
from config import config
from context import trim_tool_calls
from mcp_registry import register_named_server
from model import initialize_model
from output import Output

# configure the Phoenix tracer
tracer_provider = register(
    project_name="agents",  # Default is 'default'
    auto_instrument=True,  # Auto-instrument your app based on installed dependencies
)


async def main() -> None:
    logging.basicConfig(
        level=logging.WARNING,  # Set root logger to capture everything
        format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
    )
    logging.getLogger("agentscli").setLevel(logging.INFO)

    async with (
        MCPServerStreamableHttp(
            params={"url": "http://localhost:8002/mcp"},
            cache_tools_list=True,
        ) as mcp_server,
        MCPServerStreamableHttp(
            params={"url": "http://localhost:8002/mcp"},
            cache_tools_list=True,
            tool_filter={"allowed_tool_names": ["execute_python_code"]},
        ) as calculator_mcp_server,
    ):
        register_named_server("default", mcp_server)
        register_named_server("calculator", calculator_mcp_server)
        context = []
        output = Output()
        while True:
            # last_message = context[-1] if len(context) > 0 else None
            # print(f"last message: {last_message}")
            query = output.capture_user_input()

            context.append({"content": query, "role": "user"})

            result = await run_single(query, context, mcp_server, output)
            context = result
            context = trim_tool_calls(context)


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
        model = config.MODEL_NAME
    if not model:
        model = input("Enter a model name for Litellm: ")

    api_url = args.api_url
    if not api_url:
        api_url = config.MODEL_BASE_URI
    if not api_url:
        api_url = input("Enter an API base URL for Litellm: ")

    api_key = args.api_key
    if not api_key:
        api_key = config.MODEL_API_KEY
    if not api_key and "localhost" not in api_url:
        api_key = input("Enter an API key for Litellm: ")
    if not api_key:
        api_key = "empty"

    initialize_model(model, api_key, api_url)

    asyncio.run(main())
