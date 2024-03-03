#!/usr/bin/bash
set -e
podman run --rm --gpus all \
    --device nvidia.com/gpu=all \
    -v /home/andy/mnt/drive/.cache/huggingface:/root/.cache/huggingface \
    --env "HUGGING_FACE_HUB_TOKEN=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" \
    -p 8000:8000 \
    --ipc=host \
    vllm/vllm-openai:latest \
    --model $1 \
    --revision $2 \
    --dtype float16 \
    --max-model-len 8192
