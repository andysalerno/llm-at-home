import textwrap
import datetime
import json
from typing import Any
from jinja2 import Template
from pydantic import BaseModel
from pydantic_ai import Agent, RunContext
from pydantic_ai.tools import Tool
from pydantic_ai.settings import ModelSettings
from code_execution_tool import create_code_execution_tool
from model import create_model
from state import State
from pydantic_ai.messages import (
    ModelMessage,
    ModelRequest,
    ToolReturnPart,
)


def store_memory_tool() -> Tool:
    return Tool(
        function=_store_memory,
        name="store_memory",
        takes_ctx=True,
    )


async def _store_memory(ctx: RunContext[State], memory_fragment: str) -> str:
    pass


def search_memory_tool() -> Tool:
    return Tool(
        function=_search_memory,
        name="search_memory",
        takes_ctx=True,
    )


async def _search_memory(ctx: RunContext[State], keywords: str) -> str:
    pass


if __name__ == "__main__":
    import chromadb

    client = chromadb.HttpClient(host="localhost", port=8000)
    collection = client.create_collection(name="my_collection", get_or_create=True)
    collection.add(
        documents=[
            "This is a document about pineapple",
            "This is a document about oranges",
        ],
        ids=["id1", "id2"],
    )

    # print(collection.peek())

    result = collection.query(query_texts="citrus")
    print(result)
