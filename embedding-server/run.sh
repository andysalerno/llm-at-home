#!/usr/bin/bash
podman run \
    --gpus all \
    --device nvidia.com/gpu=all \
    --rm -d -p 8001:8000 localhost/embeddingserver