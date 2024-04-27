import json
from http.server import BaseHTTPRequestHandler, HTTPServer
from typing import Optional
import torch.nn.functional as F
from torch import Tensor
from sentence_transformers import SentenceTransformer, util
from transformers import AutoTokenizer, AutoModel


def log(message: str):
    print(message, flush=True)


# EMBEDDING_MODEL_NAME = "all-mpnet-base-v2"
# EMBEDDING_MODEL_NAME = "multi-qa-MiniLM-L6-cos-v1"
# EMBEDDING_MODEL_NAME = "all-MiniLM-L6-v2"
EMBEDDING_MODEL_NAME = "intfloat/e5-small-v2"

log("initializing embedding transformer...")
embedding_model = SentenceTransformer(EMBEDDING_MODEL_NAME)
# tokenizer = AutoTokenizer.from_pretrained(EMBEDDING_MODEL_NAME)
# model = AutoModel.from_pretrained(EMBEDDING_MODEL_NAME)
log("done.")


def transform_passage(passage: str) -> str:
    return f"passage: {passage}"


def transform_query(query: str) -> str:
    return f"query: {query}"


class MyHandler(BaseHTTPRequestHandler):
    def do_POST(self):
        log(f"POST {self.path}")

        if self.path == "/embeddings":
            self.handle_embeddings()

    def handle_scoring(self):
        log("scoring requested")
        content_length = int(self.headers["Content-Length"])
        body = json.loads(self.rfile.read(content_length).decode("utf-8"))

        text = body["input"] if "input" in body else body["text"]

        query = None
        if "query" in body:
            query = body["query"]

        (passage_embeddings, query_embedding) = get_embeddings(text, query)

        # scores = (embeddings[:2] @ embeddings[2:].T) * 100
        scores = util.dot_score(query_embedding, passage_embeddings)[0].cpu().tolist()

    def handle_embeddings(self):
        log("embeddings requested")

        content_length = int(self.headers["Content-Length"])
        body = json.loads(self.rfile.read(content_length).decode("utf-8"))

        text = body["input"] if "input" in body else body["text"]

        query = None
        if "query" in body:
            query = body["query"]

        (passage_embeddings, query_embedding) = get_embeddings(text, query)

        data = [
            {"object": "embedding", "embedding": emb, "index": n}
            for n, emb in enumerate(passage_embeddings)
        ]

        result = {
            "object": "list",
            "data": data,
            "model": EMBEDDING_MODEL_NAME,
            "usage": {
                "prompt_tokens": 0,
                "total_tokens": 0,
            },
        }

        if query_embedding is not None:
            result["query_data"] = {
                "object": "embedding",
                "embedding": query_embedding,
                "index": 0,
            }

        response = json.dumps(result)

        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.end_headers()
        self.wfile.write(response.encode("utf-8"))
        log("embeddings request completed")


def average_pool(last_hidden_states: Tensor, attention_mask: Tensor) -> Tensor:
    last_hidden = last_hidden_states.masked_fill(~attention_mask[..., None].bool(), 0.0)
    return last_hidden.sum(dim=1) / attention_mask.sum(dim=1)[..., None]


def get_embeddings(text: list[str], query: Optional[str]):
    if type(text) is str:
        text = [text]

    text = [transform_passage(t) for t in text]

    text_to_embed = text

    if query:
        text_to_embed = text[:]
        text_to_embed.append(transform_query(query))
        log(f"query in body: {query}")

    # batch_dict = tokenizer(
    #     text_to_embed,
    #     max_length=512,
    #     padding=True,
    #     truncation=True,
    #     return_tensors="pt",
    # )
    # outputs = model(**batch_dict)
    # embeddings = average_pool(outputs.last_hidden_state, batch_dict["attention_mask"])
    # embeddings = F.normalize(embeddings, p=2, dim=1)
    # embeddings = embeddings.tolist()

    embeddings = embedding_model.encode(
        text_to_embed, normalize_embeddings=True
    ).tolist()

    query_embedding = None
    log(f"len embeddings: {len(embeddings)} len text: {len(text)}")
    if len(embeddings) == len(text) + 1:
        # we added the query; need to pop it
        query_embedding = embeddings.pop()

    return (embeddings, query_embedding)


def run(server_class=HTTPServer, handler_class=MyHandler):
    log("warming up embedding model...")
    (passage_embeds, query_embed) = get_embeddings(
        [
            "Paul McCartney played bass",
            "Ringo Starr played drums",
            "Michael Jordan played basketball",
            "garbage",
            "John Lennon played guitar",
            "George Harrison played guitar",
            "The Beatles were comprised of Paul, John, George, and Ringo.",
        ],
        "Who were the members of The Beatles?",
    )
    scores = util.dot_score(query_embed, passage_embeds)[0].cpu().tolist()
    log(f"score: {scores}")
    log("done.")

    port = 8000
    server_address = ("0.0.0.0", port)
    httpd = server_class(server_address, handler_class)
    log(f"Starting httpd. Listening on port {port}...\n")
    httpd.serve_forever()


if __name__ == "__main__":
    run()
