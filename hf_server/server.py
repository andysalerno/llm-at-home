import argparse
import torch
from flask import Flask, request, jsonify
from transformers import pipeline, Pipeline, AutoModelForCausalLM, AutoTokenizer
from hqq.engine.hf import HQQModelForCausalLM
from hqq.core.quantize import HQQLinear, HQQBackend

app = Flask(__name__)


cache_dir: str = "/home/tiny/mnt/drive/.cache/huggingface"
model: Pipeline = None


def log(msg: str):
    print(msg, flush=True)


def load_hqq_model(model_name: str):
    log(f"Loading hqq model: {model_name}...")
    HQQLinear.set_backend(
        HQQBackend.ATEN
    )  # C++ Aten/CUDA backend (set automatically by default if available)
    llm = HQQModelForCausalLM.from_quantized(
        model_name, device="cuda", cache_dir=cache_dir
    )

    return llm


def load_generic_model(model_name: str):
    log(f"Loading generic model: {model_name}...")
    llm = AutoModelForCausalLM.from_pretrained(
        model_name,
        cache_dir=cache_dir,
        device_map="auto",
        torch_dtype=torch.bfloat16,
        load_in_8bit=True,
        attn_implementation="flash_attention_2",
    )

    # llm = llm.to_bettertransformer()
    return llm


def load_model(model_name: str):
    if "hqq" in model_name.lower():
        llm = load_hqq_model(model_name)
    else:
        llm = load_generic_model(model_name)

    tokenizer = AutoTokenizer.from_pretrained(
        model_name, cache_dir="/home/tiny/mnt/drive/.cache/huggingface"
    )
    log(f"Model: {model_name} has been loaded")

    log("Creating pipeline...")
    model = pipeline("text-generation", model=llm, tokenizer=tokenizer, device="cuda")
    log("Pipeline created")
    return model


@app.route("/chat-completions", methods=["POST"])
def chat_completions():
    data = request.get_json()
    user_input = data.get("prompt", "")
    max_tokens = data.get("max_tokens", 50)

    # Generate text from the model
    response = model(user_input, max_length=len(user_input.split()) + max_tokens)[0][
        "generated_text"
    ]
    return jsonify({"response": response})


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="Run a simple Huggingface Transformers server"
    )
    parser.add_argument(
        "--model",
        type=str,
        required=True,
        help="Model name to load with Huggingface Transformers",
    )
    args = parser.parse_args()

    model = load_model(args.model)

    app.run(host="0.0.0.0", port=8000)
