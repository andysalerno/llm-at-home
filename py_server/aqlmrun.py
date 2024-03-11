from transformers import AutoTokenizer, AutoModelForCausalLM


def get_model_and_tokenizer(skip_warmup: bool = False):
    model_name = "ISTA-DASLab/Mixtral-8x7B-Instruct-v0_1-AQLM-2Bit-1x16-hf"
    quantized_model = AutoModelForCausalLM.from_pretrained(
        model_name,
        trust_remote_code=True,
        torch_dtype="auto",
    ).cuda()

    tokenizer = AutoTokenizer.from_pretrained(model_name)

    if not skip_warmup:
        _ = quantized_model.generate(
            tokenizer("", return_tensors="pt")["input_ids"].cuda(), max_new_tokens=10
        )

    return (quantized_model, tokenizer)
