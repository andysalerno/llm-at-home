from typing import Dict, List
import chromadb
from chromadb.config import Settings
from chromadb.api.types import EmbeddingFunction, Documents, Embeddings
from sentence_transformers import SentenceTransformer
import uuid

client = None
_collection = None

# For certain embedding models, they expect a prefix to differentiate queries and passages:
query_prefix = "query: "
passage_prefix = "passage: "


def get_client():
    global client

    if client is None:
        print("Creating client")
        # client = chromadb.PersistentClient(path="db/chromadb.db")
        client = chromadb.Client(Settings(anonymized_telemetry=False))
        print("Client created.")

    return client


def _get_collection(model: SentenceTransformer):
    global _collection

    client = get_client()

    if _collection is None:
        print("Getting or creating collection my_collection")
        _collection = client.get_or_create_collection(
            name="my_collection",
            embedding_function=LocalSentenceTransformerEmbeddingFunction(model),
        )

        print(f"my_collection loaded, it has document count: {_collection.count()}")

    return _collection


class Memory:
    def __init__(self, model: SentenceTransformer) -> None:
        self.collection = _get_collection(model)

    # time-weighted chat history,
    # web_search cached history,
    # 'remember to do X' history
    def add_many(
        self, ids: List[str], documents: List[str], metadatas: List[Dict[str, str]]
    ):
        ids = [id if len(id) > 1 else str(uuid.uuid4()) for id in ids]
        # self.collection.add(ids, documents=documents, metadatas=metadatas)
        print(f"adding many: ids: {ids}\ndocuments: {documents}")
        self.collection.add(ids, documents=documents)

    def add(self, id: str, document: str, metadata: Dict[str, str]):
        document = f"{passage_prefix}{document}"
        # self.add_many([id], [document], [metadata])
        id = id if len(id) > 1 else str(uuid.uuid4())
        print(f"adding id: {id} document: {document}")
        self.collection.add(id, documents=document)
        print("done adding item")

    def query(self, query_text: str, n_results: int, where: Dict[str, str]):
        query_text = f"{query_prefix}{query_text}"
        result = self.collection.query(
            query_texts=query_text, n_results=n_results, where=where
        )

        result["ids"] = result["ids"][0]
        result["documents"] = result["documents"][0]
        result["metadatas"] = result["metadatas"][0]
        result["distances"] = result["distances"][0]

        return result


class LocalSentenceTransformerEmbeddingFunction(EmbeddingFunction):
    def __init__(
        self,
        model: SentenceTransformer,
    ):
        self._model = model
        self._normalize_embeddings = False

    def __call__(self, input: Documents) -> Embeddings:
        print("embedding provider invoked...")
        result = self._model.encode(
            list(input),
            convert_to_numpy=True,
            normalize_embeddings=self._normalize_embeddings,
        ).tolist()

        print("embedding call invoked, and got result")

        return result
