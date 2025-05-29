# Use the official vLLM OpenAI compatible server image as the base
FROM vllm/vllm-openai:v0.8.3

# Update gguf library to version 0.14.0
# Uninstall any existing version first to ensure a clean install
RUN pip uninstall -y gguf && \
    pip install --no-cache-dir gguf==0.14.0