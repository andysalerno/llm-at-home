FROM vllm/vllm-openai:latest

# Install:
# - bitsandbytes for additional quantization support
RUN ls /usr/local/cuda
RUN ldconfig /usr/local/cuda-$(echo $CUDA_VERSION | cut -d. -f1,2)/compat/

ENTRYPOINT ["sh", "-c", "pip install -U deepspeed && python3 -m vllm.entrypoints.openai.api_server"]