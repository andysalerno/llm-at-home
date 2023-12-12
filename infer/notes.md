## Example commands

sudo podman run --rm -v $(pwd)/pod_data:/data -p 8080:80 --gpus all --device nvidia.com/gpu=all --security-opt=label=disable -e NVIDIA_VISIBLE_DEVICES=nvidia.com/gpu=all ghcr.io/huggingface/text-generation-inference:latest --model-id openchat/openchat_3.5 --quantize eetq

sudo podman run --rm -v $(pwd)/pod_data:/data -p 8080:80 --gpus all --device nvidia.com/gpu=all --security-opt=label=disable -e NVIDIA_VISIBLE_DEVICES=nvidia.com/gpu=all ghcr.io/huggingface/text-generation-inference:latest --model-id TheBloke/openchat_3.5-AWQ  --quantize awq

echo "sudo podman run --rm -v $(pwd)/pod_data:/data -p 8080:80 --gpus all --device nvidia.com/gpu=all --security-opt=label=disable -e NVIDIA_VISIBLE_DEVICES=nvidia.com/gpu=all ghcr.io/huggingface/text-generation-inference:latest --model-id TheBloke/dolphin-2.2.1-mistral-7B-AWQ --quantize awq" > example_run.txt

sudo podman run --rm -v ~/.cache/huggingface:/root/.cache/huggingface  -p 8080:80 --gpus all --device nvidia.com/gpu=all --security-opt=label=disable -e NVIDIA_VISIBLE_DEVICES=nvidia.com/gpu=all vllm  --model TheBloke/dolphin-2.2.1-mistral-7B-AWQ --quantization awq

sudo podman run --rm -v ~/models:/data -p 8080:80 --gpus all --device nvidia.com/gpu=all --security-opt=label=disable -e NVIDIA_VISIBLE_DEVICES=nvidia.com/gpu=all ghcr.io/huggingface/text-generation-inference:latest --model-id TheBloke/dolphin-2.2.1-mistral-7B-AWQ --quantize awq --max-best-of 1 --max-input-length 4095 --max-total-tokens 4096

## Servers

### llamacpp simple server
- trivial, maybe too trivial

### huggingface text generation inference
- awq (4bit), eetq (8bit)
- bug with stop tokens that don't align on token boundaries

### vllm
- supports an openai-API compatible server

### easyllm
- just a python client that provides a unified client for different backends (openai, huggingface tgi) 
- rag example: https://philschmid.github.io/easyllm/examples/llama2-rag-example/
- auto-formats chats based on the template in the tokenizer file

## ideas
build an 'intent tree' like

```
/converse
    /small-talk
/find-info
    /weather
    /wikipedia
    /google
/take-action
    /smart-home
        /lights-off
```