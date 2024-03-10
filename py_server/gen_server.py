import json
import time
import uuid
from urllib.parse import urlparse, parse_qs
from http.server import BaseHTTPRequestHandler, HTTPServer
from aqlmrun import get_model_and_tokenizer
from transformers import TextIteratorStreamer, StoppingCriteriaList, StoppingCriteria
from threading import Thread

model, tokenizer = get_model_and_tokenizer()


class StoppingStringStopCriteria(StoppingCriteria):
    def __init__(self, target_sequence, prompt):
        self.target_sequence = target_sequence
        self.prompt = prompt

    def __call__(self, input_ids, scores, **kwargs):
        generated_text = tokenizer.decode(input_ids[0])
        generated_text = generated_text.replace(self.prompt, "")
        if self.target_sequence in generated_text:
            print(
                f"stopping because generated text {generated_text} contains stop sequence '{self.target_sequence}'",
                flush=True,
            )
            return True

        return False

    def __len__(self):
        return 1

    def __iter__(self):
        yield self


def get_response_streamer(request: str):
    streamer = TextIteratorStreamer(tokenizer, skip_prompt=True)
    body = json.loads(request)

    inputs = body["inputs"]
    parameters = body["parameters"]

    # https://huggingface.co/docs/transformers/v4.18.0/en/main_classes/text_generation#transformers.generation_utils.GenerationMixin.generate
    tokenized_inputs = tokenizer([inputs], return_tensors="pt")["input_ids"].cuda()

    stopping_criteria = []
    for stopping_string in parameters["stop"]:
        stopping_criteria.append(StoppingStringStopCriteria(stopping_string, inputs))

    generation_kwargs = dict(
        inputs=tokenized_inputs,
        streamer=streamer,
        stopping_criteria=StoppingCriteriaList(stopping_criteria),
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
            parameters["max_new_tokens"] if "max_new_tokens" in parameters else "100"
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
        typical_p=float(parameters["typical_p"]) if "typical_p" in parameters else None,
    )

    thread = Thread(target=model.generate, kwargs=generation_kwargs)
    thread.start()

    return streamer


class MyHandler(BaseHTTPRequestHandler):
    def do_POST(self):
        print(f"POST {self.path}", flush=True)

        if self.path == "/generate":
            self.handle_generate()
        elif self.path == "/generate_stream":
            self.handle_generate_stream()

    def do_GET(self):
        print(f"GET {self.path}", flush=True)

        if self.path == "/info":
            self.handle_info()

    def handle_info(self):
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.end_headers()

        response = json.dumps(
            dict(
                model_id="mistral",
                model_dtype="float16",
                model_device_type="gpu",
                max_concurrent_requests=1,
                max_best_of=1,
                max_stop_sequences=1,
                max_input_length=8096,
                max_total_tokens=8096,
                waiting_served_ratio=1.0,
                max_batch_total_tokens=8096,
                max_waiting_tokens=8096,
                validation_workers=1,
                version="1",
                sha="1",
                docker_label="fake",
            )
        )

        self.wfile.write(response.encode("utf-8"))

    def handle_generate_stream(self):
        self.send_response(200)
        self.send_header("Content-Type", "text/event-stream")
        self.send_header("Cache-Control", "no-cache")
        self.send_header("Connection", "keep-alive")
        self.end_headers()

        content_length = int(self.headers["Content-Length"])
        request = self.rfile.read(content_length).decode("utf-8")
        print(f"saw request: {request}", flush=True)
        streamer = get_response_streamer(request)

        for new_text in streamer:
            if not new_text:
                continue
            print(f"saw text: '{new_text}'", flush=True)
            message_json = self.make_stream_json(new_text)
            message = f"event: tokens\ndata: {message_json}\n\n"
            print(f"sending text: '{message}'", flush=True)
            self.wfile.write(message.encode())
            self.wfile.flush()

        self.finish()
        print("done streaming", flush=True)

    def make_stream_json(self, text: str) -> str:
        return json.dumps(
            dict(
                details=None,
                generated_text=text,
                token=dict(id=0, logprob=0, special=False, text=text),
            )
        )

    def handle_generate(self):
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.end_headers()

        content_length = int(self.headers["Content-Length"])
        request = self.rfile.read(content_length).decode("utf-8")
        streamer = get_response_streamer(request)

        streamed_outputs = []

        for new_text in streamer:
            streamed_outputs.append(new_text)
            print(f"saw text: {new_text}", flush=True)

        response = json.dumps({"generated_text": "".join(streamed_outputs)})

        self.wfile.write(response.encode("utf-8"))


def run(server_class=HTTPServer, handler_class=MyHandler):
    # port = 8000
    port = 8080
    server_address = ("0.0.0.0", port)
    httpd = server_class(server_address, handler_class)
    print(f"Starting httpd. Listening on port {port}...\n", flush=True)
    httpd.serve_forever()


if __name__ == "__main__":
    run()
