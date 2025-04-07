from dataclasses import dataclass
import textwrap
import datetime
import json
from typing import Any, Optional
import chromadb
from chromadb import Collection
from jinja2 import Template
from pydantic import BaseModel
from pydantic_ai import Agent, RunContext
from pydantic_ai.tools import Tool
from pydantic_ai.settings import ModelSettings
from tools.code_execution_tool import create_code_execution_tool
from model import create_model
from state import State
from pydantic_ai.messages import (
    ModelMessage,
    ModelRequest,
    ToolReturnPart,
)
from chromadbx import UUIDGenerator
import logging
import uuid

logger = logging.getLogger(__name__)


@dataclass
class MemoryClient:
    collection: Collection

    async def store_memory(self, memory_fragment: str) -> None:
        """
        Stores a memory fragment in the database.

        Args:
            memory_fragment: The memory fragment to store.
        """
        logger.info(f"Storing memory fragment: {memory_fragment}")
        self.collection.add(documents=memory_fragment, ids=str(uuid.uuid4()))

        return "(memory stored)"

    async def search_memory(self, keywords: str) -> list[str]:
        """
        Searches the memory database for relevant memory fragments based on the keyword(s).

        Args:
            keywords: The keyword(s) to search for.

        Returns:
            A list of relevant memory fragments.
        """
        pass


def store_memory_tool(description: Optional[str] = None) -> Tool:
    description = description or (
        "Stores a simple (1-2 sentence) memory about the user."
        " For instance, 'user likes pineapple on pizza' or 'user is a software engineer', 'user's name is John Doe'."
        " Invoke this tool when you want to remember something about the user; memories can be used in future conversations."
    )

    client = chromadb.HttpClient(host="localhost", port=8000)
    collection = client.create_collection(name="my_collection", get_or_create=True)
    memory_client = MemoryClient(collection=collection)

    return Tool(
        function=memory_client.store_memory,
        description=description,
        name="store_memory",
        takes_ctx=False,
    )


def search_memory_tool() -> Tool:
    client = chromadb.HttpClient(host="localhost", port=8000)
    collection = client.create_collection(name="my_collection", get_or_create=True)
    memory_client = MemoryClient(collection=collection)

    return Tool(
        function=memory_client.search_memory,
        name="search_memory",
        takes_ctx=False,
    )


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

    result = collection.query(query_texts="citrus")
    print(result)
