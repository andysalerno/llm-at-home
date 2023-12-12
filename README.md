This is a monorepo composed of small services that combine to make a self-hostable ChatGPT-like experience. It supports custom functions to extend the abilities of the model.

A Docker Compose file exists to easily launch the services, or to see how they can be manually started.

## embedding-server

A simple Python server that exposes an endpoint `/embeddings` for getting text embeddings.

## infer

This is a proxy for text-generation-inference, but adds some useful things on top:

* A function calling convention (trivial but working)
* Template-based infilling, inspired by Guidance (much more trivial than Guidance but very useful)

This service adds those things and then re-exposes itself using the OpenAI API.

## chat-ui

This is Huggingface's project chat-ui, with a slightly modified Dockerfile.

## text-generation-inference

This is Huggingface's project text-generation-inference. Its responsibility is to host the model and expose an API for inference that is super robust, super fast, super efficient, and super easy to set up. It is similar to vllm, but exposes a slightly different API.