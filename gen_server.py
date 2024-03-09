import json
import uuid
from urllib.parse import urlparse, parse_qs
from http.server import BaseHTTPRequestHandler, HTTPServer
from aqlmrun import get_model_and_tokenizer
from transformers import TextIteratorStreamer
from threading import Thread

model, tokenizer = get_model_and_tokenizer()


class MyHandler(BaseHTTPRequestHandler):
    def do_POST(self):
        print(f"POST {self.path}", flush=True)

        if self.path == "/generate":
            self.handle_generate()

    def handle_generate(self):
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.end_headers()

        streamer = TextIteratorStreamer(tokenizer)

        content_length = int(self.headers["Content-Length"])
        body = json.loads(self.rfile.read(content_length).decode("utf-8"))

        inputs = body["inputs"]
        parameters = body["parameters"]

        # https://huggingface.co/docs/transformers/v4.18.0/en/main_classes/text_generation#transformers.generation_utils.GenerationMixin.generate
        tokenized_inputs = tokenizer([inputs], return_tensors="pt")["input_ids"].cuda()

        generation_kwargs = dict(
            inputs=tokenized_inputs,
            streamer=streamer,
            # best_of=int(parameters["best_of"] if "best_of" in parameters else "1"),
            do_sample=bool(
                parameters["do_sample"] if "do_sample" in parameters else "false"
            ),
            # frequency_penalty=float(
            #     parameters["frequency_penalty"]
            #     if "frequency_penalty" in parameters
            #     else "0"
            # ),
            max_new_tokens=int(
                parameters["max_new_tokens"]
                if "max_new_tokens" in parameters
                else "100"
            ),
            repetition_penalty=float(
                parameters["repetition_penalty"]
                if "repetition_penalty" in parameters
                else "1"
            ),
            temperature=int(parameters["temperature"])
            if "temperature" in parameters
            else None,
            top_k=int(parameters["top_k"]) if "top_k" in parameters else None,
            top_p=float(parameters["top_p"]) if "top_p" in parameters else None,
            typical_p=float(parameters["typical_p"])
            if "typical_p" in parameters
            else None,
        )

        thread = Thread(target=model.generate, kwargs=generation_kwargs)
        thread.start()

        streamed_outputs = []

        for new_text in streamer:
            streamed_outputs.append(new_text)
            print(f"saw text: {new_text}", flush=True)

        response = json.dumps({"generated_text": "".join(streamed_outputs)})

        self.wfile.write(response.encode("utf-8"))


def run(server_class=HTTPServer, handler_class=MyHandler):
    port = 8000
    server_address = ("0.0.0.0", port)
    httpd = server_class(server_address, handler_class)
    print(f"Starting httpd. Listening on port {port}...\n", flush=True)
    httpd.serve_forever()


if __name__ == "__main__":
    run()
