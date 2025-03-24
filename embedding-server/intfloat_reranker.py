from embedding_server import log
from sentence_transformers import SentenceTransformer, util

EMBEDDING_MODEL_NAME = "intfloat/e5-small-v2"


def _transform_passage(passage: str) -> str:
    return f"passage: {passage}"


def _transform_query(query: str) -> str:
    return f"query: {query}"


class IntFloatReranker:
    def __init__(self):
        log(f"initializing embedding model: {EMBEDDING_MODEL_NAME}...")
        self.embedding_model = SentenceTransformer(EMBEDDING_MODEL_NAME)
        log("done.")

    def model_name(self) -> str:
        return EMBEDDING_MODEL_NAME

    def get_scores(self, query: str, corpus: list[str]) -> list[float]:
        (passage_embeddings, query_embedding) = self.get_embeddings(query, query)

        # scores = (embeddings[:2] @ embeddings[2:].T) * 100
        scores = util.dot_score(query_embedding, passage_embeddings)[0].cpu().tolist()

        return scores

    def get_embeddings(self, query: str, corpus: str | list[str]):
        if type(corpus) is str:
            corpus = [corpus]

        corpus = [_transform_passage(t) for t in corpus]

        text_to_embed = corpus

        if query:
            text_to_embed = corpus[:]
            text_to_embed.append(_transform_query(query))
            log(f"query in body: {query}")

        embeddings = self.embedding_model.encode(
            text_to_embed, normalize_embeddings=True
        ).tolist()

        query_embedding = None
        if len(embeddings) == len(corpus) + 1:
            # we added the query; need to pop it
            query_embedding = embeddings.pop()

        return (embeddings, query_embedding)
