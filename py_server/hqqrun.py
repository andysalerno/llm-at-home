from hqq.core.quantize import *


def get_model_and_tokenizer(skip_warmup: bool = False):
    model_id = "mobiuslabsgmbh/Mixtral-8x7B-Instruct-v0.1-hf-attn-4bit-moe-3bit-metaoffload-HQQ"
    from hqq.engine.hf import HQQModelForCausalLM, AutoTokenizer

    tokenizer = AutoTokenizer.from_pretrained(model_id)
    tokenizer.use_default_system_prompt = False
    model = HQQModelForCausalLM.from_quantized(model_id)

    # Optional: set backend/compile
    # You will need to install CUDA kernels apriori
    # git clone https://github.com/mobiusml/hqq/
    # cd hqq/kernels && python setup_cuda.py install

    HQQLinear.set_backend(HQQBackend.ATEN)

    return (model, tokenizer)
