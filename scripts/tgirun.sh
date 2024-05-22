#!/usr/bin/bash
set -e
# QUANTIZATION=eetq
QUANTIZATION=bitsandbytes
# MAX_TOKENS=8192
# MAX_INPUT=8191

MODEL_ID=microsoft/Phi-3-small-8k-instruct
# MODEL_REVISION=9619fb7d2a8e25fa6b0633c0f57f7f4aa79b45c4

podman run --rm --gpus all \
    --device nvidia.com/gpu=all \
    -v /home/tiny/mnt/drive/.cache/huggingface:/data \
    -p 8080:8000 \
    ghcr.io/huggingface/text-generation-inference:latest \
    --model-id ${MODEL_ID} \
    --port 8000 \
    --quantize ${QUANTIZATION} \
    # --max-best-of 1 \
    # --max-concurrent-requests 2 \
    # --validation-workers 1 \
    # --max-batch-prefill-tokens ${MAX_TOKENS} \
    # --max-total-tokens ${MAX_TOKENS} \
    # --max-input-length ${MAX_INPUT} \
    # --max-batch-total-tokens ${MAX_TOKENS} \
    # --max-top-n-tokens 1 \
    # --revision ${MODEL_REVISION}