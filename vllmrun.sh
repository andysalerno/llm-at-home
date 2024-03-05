#!/usr/bin/bash
set -e
podman run --rm --gpus all \
    --device nvidia.com/gpu=all \
    -v /home/tiny/.cache/huggingface:/root/.cache/huggingface \
    -p 8000:8000 \
    vllm/vllm-openai:latest \
    --model $1 \
    --max-model-len 8192
