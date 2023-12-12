import json
import uuid
from urllib.parse import urlparse, parse_qs
from http.server import BaseHTTPRequestHandler, HTTPServer
from sentence_transformers import SentenceTransformer
import chromadb


# EMBEDDING_MODEL_NAME = "all-mpnet-base-v2"
# EMBEDDING_MODEL_NAME = "multi-qa-MiniLM-L6-cos-v1"
# EMBEDDING_MODEL_NAME = "all-MiniLM-L6-v2"
EMBEDDING_MODEL_NAME = "intfloat/e5-small-v2"

print("initializing embedding transformer...")
embedding_model = SentenceTransformer(EMBEDDING_MODEL_NAME)
print("done.")

chroma_client = chromadb.PersistentClient(path="./chroma")
collection = chroma_client.create_collection(
    name="my_collection", get_or_create=True, embedding_function=None
)


class MyHandler(BaseHTTPRequestHandler):
    def do_POST(self):
        print(f"POST {self.path}", flush=True)

        if self.path == "/embeddings":
            self.handle_embeddings()
        elif self.path == "/memory":
            self.handle_memory_post()

    def do_GET(self):
        print(f"GET {self.path}", flush=True)

        if self.path.startswith("/memory"):
            self.handle_memory_get()

    def handle_memory_get(self):
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.end_headers()

        parsed_url = urlparse(self.path)
        query_params = parse_qs(parsed_url.query)

        print(f"params: {query_params}")

        input = query_params["input"][0]
        num_results = int(query_params["num_results"][0])

        print(f"querying {num_results} for input {input}")

        embeddings = embedding_model.encode(input).tolist()

        results = collection.query(query_embeddings=embeddings, n_results=num_results)

        print(f"got results: {results}")

        response = json.dumps(results)

        self.wfile.write(response.encode("utf-8"))

    def handle_memory_post(self):
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.end_headers()

        content_length = int(self.headers["Content-Length"])
        body = json.loads(self.rfile.read(content_length).decode("utf-8"))

        document = body["document"]

        embeddings = embedding_model.encode(document).tolist()

        uuid_str = str(uuid.uuid4())

        collection.add(
            embeddings=embeddings, documents=document, metadatas=None, ids=uuid_str
        )

    def handle_embeddings(self):
        print("embeddings requested")
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.end_headers()

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

        self.wfile.write(response.encode("utf-8"))


def run(server_class=HTTPServer, handler_class=MyHandler):
    print("warming up embedding model...", flush=True)
    embedding_model.encode("blank text to trigger deployment to GPU")
    print("done.", flush=True)

    port = 8000
    server_address = ("0.0.0.0", port)
    httpd = server_class(server_address, handler_class)
    print(f"Starting httpd. Listening on port {port}...\n", flush=True)
    httpd.serve_forever()


if __name__ == "__main__":
    run()
