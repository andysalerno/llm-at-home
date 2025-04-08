import logging
import uuid
from dataclasses import dataclass

import chromadb
from chromadb import Collection
from pydantic_ai.tools import Tool

logger = logging.getLogger(__name__)


@dataclass
class MemoryClient:
    collection: Collection

    async def store_memory(self, memory_fragment: str) -> str:
        """
        Stores a memory fragment in the database.

        Args:
            memory_fragment: The memory fragment to store.
        """
        logger.info("Storing memory fragment: %s", {memory_fragment})
        self.collection.add(documents=memory_fragment, ids=str(uuid.uuid4()))

        return "(memory stored)"

    async def search_memory(self, keyword: str) -> list[str]:
        """
        Searches the memory database for relevant memory fragments based on the keyword.

        Args:
            keyword: The keyword to search for.

        Returns:
            A list of relevant memory fragments, or an empty list if none are found.
        """
        logger.info("Searching memory with keyword: %s", keyword)
        results = self.collection.query(query_texts=keyword, n_results=5)
        logger.info("Search results: %s", results)

        documents = results["documents"]
        documents = documents[0] if isinstance(documents, list) else documents

        return documents or []


def store_memory_tool(
    collection_name: str | None = None,
    description: str | None = None,
) -> Tool:
    description = description or (
        "Stores a simple (1-2 sentence) memory about the user."
        " For instance, 'user likes pineapple on pizza' or 'user is a software engineer', 'user's name is John Doe'."
        " Invoke this tool when you learn something about the user; memories can be used in future conversations."
    )
    collection_name = collection_name or "my_collection"

    client = chromadb.HttpClient(host="localhost", port=8000)
    collection = client.create_collection(name=collection_name, get_or_create=True)
    memory_client = MemoryClient(collection=collection)

    return Tool(
        function=memory_client.store_memory,
        description=description,
        name="store_memory",
        takes_ctx=False,
    )


def search_memory_tool(
    collection_name: str | None = None,
    description: str | None = None,
) -> Tool:
    collection_name = collection_name or "my_collection"
    description = description or (
        "Searches the memory database for relevant memory fragments based on the keyword."
        " For instance, 'favorite color' may yield documents related to the user's favorite color."
        " 'hobbies' may yield documents related to the user's hobbies."
        " Invoke this tool when you want to recall something about the user."
    )

    client = chromadb.HttpClient(host="localhost", port=8000)
    collection = client.create_collection(name=collection_name, get_or_create=True)
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
    logger.info(result)
