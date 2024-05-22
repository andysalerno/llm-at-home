from flask import Flask, request, jsonify
import torch
from transformers import (
    AutoModelForCausalLM,
    AutoTokenizer,
    BitsAndBytesConfig,
    pipeline,
    Pipeline,
    TextGenerationPipeline,
)

model_id = "microsoft/Phi-3-small-8k-instruct"


def log(msg: str):
    print(msg, flush=True)


def load_model(model_id: str, load_in_8bit: bool = True) -> AutoModelForCausalLM:
    log(f"loading model: {model_id}")
    assert torch.cuda.is_available(), "This model needs a GPU to run ..."
    torch.random.manual_seed(0)
    quant_config = None

    if load_in_8bit:
        quant_config = BitsAndBytesConfig(load_in_8bit=True)

    model = AutoModelForCausalLM.from_pretrained(
        model_id,
        torch_dtype="auto",
        trust_remote_code=True,
        quantization_config=quant_config,
    )

    log("done, model loaded.")

    return model


def get_pipeline(model_id: str, load_in_8bit: bool = True) -> TextGenerationPipeline:
    log("getting pipeline...")
    model = load_model(model_id, load_in_8bit)

    device = None
    if not load_in_8bit:
        device = torch.cuda.current_device()
        model = model.to(device)

    tokenizer = AutoTokenizer.from_pretrained(model_id, trust_remote_code=True)

    pipe: TextGenerationPipeline = pipeline(
        "text-generation",
        model=model,
        tokenizer=tokenizer,
        device=device,
        trust_remote_code=True,
    )

    log("got pipe")

    return pipe


def test():
    messages = [
        {
            "role": "user",
            "content": "Can you provide ways to eat combinations of bananas and dragonfruits?",
        },
        {
            "role": "assistant",
            "content": "Sure! Here are some ways to eat bananas and dragonfruits together: 1. Banana and dragonfruit smoothie: Blend bananas and dragonfruits together with some milk and honey. 2. Banana and dragonfruit salad: Mix sliced bananas and dragonfruits together with some lemon juice and honey.",
        },
        {"role": "user", "content": "What about solving an 2x + 3 = 7 equation?"},
    ]

    generation_args = {
        "max_new_tokens": 500,
        "return_full_text": False,
        "temperature": 0.0,
        "do_sample": False,
    }

    pipe = get_pipeline(model_id)

    test_1_output = run_inference(pipe, messages, **generation_args)
    log(f"got output: {test_1_output}")

    test_2_output = run_inference(pipe, messages, **generation_args)
    log(f"got output: {test_2_output}")

    test_3_output = run_inference(pipe, messages, **generation_args)
    log(f"got output: {test_3_output}")

    messages = [
        {
            "role": "user",
            "content": "Hi! Can you tell me a joke about penguins?",
        },
    ]
    test_4_output = run_inference(pipe, messages, **generation_args)
    log(f"got output: {test_4_output}")


def run_inference(
    pipeline: Pipeline, messages: list[dict[str, str]], **generation_args
) -> str:
    log("running inference...")
    output = pipeline(messages, **generation_args)
    log("inference complete")
    return output[0]["generated_text"]


if __name__ == "__main__":
    test()
