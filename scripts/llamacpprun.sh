#!/usr/bin/bash
set -e

podman run --rm --gpus all \
    --device nvidia.com/gpu=all \
    -v /home/tiny/mnt/drive/gguf:/models \
    -p 8003:8080 \
    localhost/llama.cpp:server-cuda \
    -m /models/Phi-3-mini-128k-instruct.Q8_0.gguf --port 8080 --host 0.0.0.0 -ngl 100 -fa