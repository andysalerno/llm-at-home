FROM vllm/vllm-openai:latest

# Install vllm nightly build
# RUN uv pip install -U vllm \
#     --torch-backend=auto \
#     --system \
#     --extra-index-url https://wheels.vllm.ai/nightly

RUN uv pip install -U vllm==0.10.1+gptoss \
    --system \
    --extra-index-url https://wheels.vllm.ai/gpt-oss/ \
    --extra-index-url https://download.pytorch.org/whl/nightly/cu128 \
    --index-strategy unsafe-best-match \
    --prerelease=allow