from http.server import BaseHTTPRequestHandler, HTTPServer
import json
import transformers
from transformers import pipeline
from threading import Thread
from huggingface_hub import create_inference_endpoint

model_id = (
    "mobiuslabsgmbh/Mixtral-8x7B-Instruct-v0.1-hf-attn-4bit-moe-3bit-metaoffload-HQQ"
)
# Load the model
from hqq.engine.hf import HQQModelForCausalLM, AutoTokenizer

tokenizer = AutoTokenizer.from_pretrained(model_id)
tokenizer.use_default_system_prompt = False
model = HQQModelForCausalLM.from_quantized(model_id)

# Optional: set backend/compile
# You will need to install CUDA kernels apriori
# git clone https://github.com/mobiusml/hqq/
# cd hqq/kernels && python setup_cuda.py install
from hqq.core.quantize import *

HQQLinear.set_backend(HQQBackend.ATEN)

pipe = pipeline("text-generation", model, tokenizer=tokenizer, device="cuda")

messages = [
    {
        "role": "user",
        "content": "Please summarize the following article: The Hyundai Ioniq 5 enters this competition with a leg up, having taken home MotorTrend's 2023 SUV of the Year award. The biggest question we weighed when naming it the winner was whether or not the Ioniq 5 is actually an SUV. We decided in fact it was, and the rest was easy, with us calling it a \"game-changing rethink of what an SUV can be.\" Taking one out for a refresher drive around the canyons of Malibu did nothing but reinforce how right we were. It's just a great-driving vehicle. The Ioniq 5 Limited we're comparing to the Tesla Model Y comes packing two motors and a combined 320 horsepower and 446 lb-ft of torque. It features a 77.4-kWh battery pack and an EPA-rated range of 266 miles. Because of an 800-volt electrical architecture, the Ioniq 5 can take advantage of 350-kW chargers and go from 10 to 80 percent charge in 18 minutes. That's quick. As tested, this loaded Hyundai will set you back $58,045.\n\nThe Tesla Model Y also comes with two motors, good for a combined 384 hp and 375 lb-ft of torque. The Model Y has a larger 84.6-kWh battery, with an EPA-rated range of 330 miles. However, its more primitive 400-volt electrical architecture means it charges more slowly than the Hyundai. Figure on 40 minutes to go from 10 to 80 percent full at a Tesla Supercharger station. It's hard to pinpoint exactly how much a Tesla costs, as weekly if not daily price updates—often in the form of cuts—seem to be the norm. Also, since Tesla killed off its communications department there's no way to borrow press cars, like we do with every other carmaker. As such, we rented one, and its as-tested price was $70,130. That said, a good chunk of that came from Tesla's optional (and rightly controversial) Full Self-Driving capability, which costs $15,000 and cannot in fact self-drive. Moreover, we couldn't even get it to turn on, but our hunch is that whoever rented us the car disabled it, so we subtracted its cost from our price.",
    },
]
# prompt = pipe.tokenizer.apply_chat_template(
#     messages, tokenize=False, add_generation_prompt=True
# )
# print(pipe(prompt, max_new_tokens=128)[0]["generated_text"])


def chat_processor(chat, max_new_tokens=100, do_sample=False):
    tokenizer.use_default_system_prompt = False
    streamer = transformers.TextIteratorStreamer(
        tokenizer, timeout=10.0, skip_prompt=True, skip_special_tokens=True
    )

    generate_params = dict(
        tokenizer("<s> [INST] " + chat + " [/INST] ", return_tensors="pt").to("cuda"),
        streamer=streamer,
        max_new_tokens=max_new_tokens,
        do_sample=do_sample,
        top_p=0.90,
        top_k=50,
        temperature=0.6,
        num_beams=1,
        repetition_penalty=1.2,
    )

    # model.generate(**generate_params)

    t = Thread(target=model.generate, kwargs=generate_params)
    t.start()
    outputs = []
    for text in streamer:
        outputs.append(text)
        print(text, end="", flush=True)

    return outputs


################################################################################################
# Generation
# outputs = chat_processor(
#     "How can I write a function in Rust for reversing a string?",
#     max_new_tokens=1000,
#     do_sample=False,
# )

class MyHandler(BaseHTTPRequestHandler):
    def do_POST(self):
        print(f"POST {self.path}", flush=True)

        self.handle_completion()


    def handle_completion(self):
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.end_headers()

        content_length = int(self.headers["Content-Length"])
        body = json.loads(self.rfile.read(content_length).decode("utf-8"))

        print(f'saw request: {body}')

        messages = body['messages']

        print(f'saw messages: {messages}')

        prompt = pipe.tokenizer.apply_chat_template(
            messages, tokenize=False, add_generation_prompt=True
        )
        print(f'prompting with:\n{prompt}')
        print('generating...', flush=True)
        generated_text = pipe(prompt, max_new_tokens=128)[0]['generated_text']
        generated_text = generated_text[len(prompt):]
        print('...done.', flush=True)
        print(f'generated:\n{generated_text}', flush=True)

        response = json.dumps(
            {
                "choices": [{'message': {'content': generated_text}}]
            }
        )

        self.wfile.write(response.encode("utf-8"))


def run(server_class=HTTPServer, handler_class=MyHandler):
    port = 8000
    server_address = ("0.0.0.0", port)
    httpd = server_class(server_address, handler_class)
    print(f"Starting httpd. Listening on port {port}...\n", flush=True)
    httpd.serve_forever()


if __name__ == "__main__":
    run()
