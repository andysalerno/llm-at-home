#!/usr/bin/bash
set -e

podman run --rm --gpus all \
    --device nvidia.com/gpu=all \
    -v /home/tiny/mnt/drive/.cache/huggingface:/root/.cache/huggingface \
    -p 8000:8000 \
    vllm/vllm-openai:latest \
    --model $1 \
    --served-model-name model \
    --max-model-len 8192 \
    -tp 1 -pp 1 --trust-remote-code
