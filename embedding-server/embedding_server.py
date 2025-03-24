import json
import os
from log import log
from http.server import BaseHTTPRequestHandler, HTTPServer
from typing import List, Optional
from sentence_transformers import util
from intfloat_reranker import IntFloatReranker
from mixedbread_reranker import MixedBreadReranker


reranker = None

if os.environ.get("RERANKER_NAME") == "mixedbread":
    log("using mixedbread reranker")
    reranker = MixedBreadReranker()
elif os.environ.get("RERANKER_NAME") == "intfloat":
    log("using intfloat reranker")
    reranker = IntFloatReranker()
else:
    raise ValueError(
        "Please set the environment variable 'RERANKER_NAME' to either 'mixedbread' or 'intfloat'"
    )


class MyHandler(BaseHTTPRequestHandler):
    def do_POST(self):
        log(f"POST {self.path}")

        if self.path == "/embeddings":
            self.handle_embeddings()
        elif self.path == "/scores":
            self.handle_scoring()

    def extract_from_request(self) -> tuple[List[str], Optional[str]]:
        content_length = int(self.headers["Content-Length"])
        body = json.loads(self.rfile.read(content_length).decode("utf-8"))

        text = body["input"] if "input" in body else body["text"]

        query = None
        if "query" in body:
            query = body["query"]
            log(f'saw request with query: "{query}"')
        else:
            log("request had no query")

        return (text, query)

    def handle_scoring(self):
        log("scoring requested")
        (corpus, query) = self.extract_from_request()

        log("corpus length: " + str(len(corpus)))

        if query is None:
            raise ValueError("query is required for scoring")

        scores = reranker.get_scores(query, corpus)

        response = json.dumps({"scores": scores})

        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.end_headers()
        self.wfile.write(response.encode("utf-8"))
        log("scores request completed")

    def handle_embeddings(self):
        log("embeddings requested")
        (corpus, query) = self.extract_from_request()

        if query is None:
            raise ValueError("query is required for scoring")

        (passage_embeddings, query_embedding) = reranker.get_embeddings(query, corpus)

        data = [
            {"object": "embedding", "embedding": emb, "index": n}
            for n, emb in enumerate(passage_embeddings)
        ]

        result = {
            "object": "list",
            "data": data,
            "model": reranker.model_name(),
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


def run(server_class=HTTPServer, handler_class=MyHandler):
    log("warming up embedding model...")
    passages = [
        "Paul McCartney played bass",
        "Ringo Starr played drums",
        "Michael Jordan played basketball",
        "garbage",
        "John Lennon played guitar",
        "George Harrison played guitar",
        "The Beatles were comprised of Paul, John, George, and Ringo.",
    ]
    query = "Who were the members of The Beatles?"
    scores = reranker.get_scores(query, passages)
    log(f"score: {scores}")
    log("done.")

    port = 8000
    server_address = ("0.0.0.0", port)
    httpd = server_class(server_address, handler_class)
    log(f"Starting httpd. Listening on port {port}...\n")
    httpd.serve_forever()


if __name__ == "__main__":
    run()
