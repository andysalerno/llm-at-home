from log import log
from sentence_transformers import CrossEncoder

EMBEDDING_MODEL_NAME = "mixedbread-ai/mxbai-rerank-xsmall-v1"


class MixedBreadReranker:
    def __init__(self):
        log(f"initializing embedding model: {EMBEDDING_MODEL_NAME}...")
        self.embedding_model = CrossEncoder(EMBEDDING_MODEL_NAME)
        log("done.")

    def model_name(self) -> str:
        return EMBEDDING_MODEL_NAME

    def get_scores(self, query: str, corpus: list[str]) -> list[float]:
        results = self.embedding_model.rank(
            query,
            corpus,
        )

        # order results by results['corpus_id']:
        results = sorted(results, key=lambda x: x["corpus_id"])

        return [rank["score"].item() for rank in results]

    def get_embeddings(self, query: str, corpus: str | list[str]):
        raise NotImplementedError(
            "get_embeddings is not implemented for MixedBreadReranker"
        )
