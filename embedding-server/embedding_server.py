import json
from http.server import BaseHTTPRequestHandler, HTTPServer
from sentence_transformers import SentenceTransformer


def log(message: str):
    print(message, flush=True)


# EMBEDDING_MODEL_NAME = "all-mpnet-base-v2"
# EMBEDDING_MODEL_NAME = "multi-qa-MiniLM-L6-cos-v1"
# EMBEDDING_MODEL_NAME = "all-MiniLM-L6-v2"
EMBEDDING_MODEL_NAME = "intfloat/e5-small-v2"

log("initializing embedding transformer...")
embedding_model = SentenceTransformer(EMBEDDING_MODEL_NAME)
log("done.")


class MyHandler(BaseHTTPRequestHandler):
    def do_POST(self):
        log(f"POST {self.path}")

        if self.path == "/embeddings":
            self.handle_embeddings()

    def handle_embeddings(self):
        log("embeddings requested")

        content_length = int(self.headers["Content-Length"])
        body = json.loads(self.rfile.read(content_length).decode("utf-8"))

        text = body["input"] if "input" in body else body["text"]

        if type(text) is str:
            text = [text]

        embeddings = embedding_model.encode(text).tolist()

        data = [
            {"object": "embedding", "embedding": emb, "index": n}
            for n, emb in enumerate(embeddings)
        ]

        response = json.dumps(
            {
                "object": "list",
                "data": data,
                "model": EMBEDDING_MODEL_NAME,
                "usage": {
                    "prompt_tokens": 0,
                    "total_tokens": 0,
                },
            }
        )

        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.end_headers()
        self.wfile.write(response.encode("utf-8"))
        log("embeddings request completed")


def run(server_class=HTTPServer, handler_class=MyHandler):
    log("warming up embedding model...")
    embedding_model.encode("blank text to trigger deployment to GPU")
    log("done.")

    port = 8000
    server_address = ("0.0.0.0", port)
    httpd = server_class(server_address, handler_class)
    log(f"Starting httpd. Listening on port {port}...\n")
    httpd.serve_forever()


if __name__ == "__main__":
    run()
