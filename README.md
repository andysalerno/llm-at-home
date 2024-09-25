# Monorepo Overview
This is a monorepo composed of small projects I have been working on related to self-hosting and serving LLMs.

These projects can combine to make a self-hostable, extensible, customizable, ChatGPT-like experience. It supports custom functions to extend the abilities of the model.

A Docker Compose file exists to easily launch the services, or to see how they can be manually started.

These are the top-level services:

- **AgentFlow**: A framework for building Agents in c# in a purely composable way. Eventually will be rewritten in Rust, like Infer.
- **Infer**: A service in Rust for adding function calling and templating on top of a local LLM.

All other projects are libraries or services that support the functionality of those two.

## agentflow (new)

This is a c# project which consists of a framework for building AI agents.

It has an examples dir that shows how agents may be built using the framework.

TODO: rewrite in Rust to go alongside the Infer library :)

TODO: tree of thought, where every tool is in a taxonomy-tree like hierarchy, which the model can navigate without getting overwhelmed by choices

~~TODO: package as a Docker container.~~ Done :)

~~TODO: accept system prompt from callers, because Huggingface ChatUI sends a system prompt to make a title for the conversation. But need to distinguish when to replace or not...~~ Done :)

## scraper

A very simple http service written in Rust that accepts a list of URLs via POST request, and returns the webpage content at those urls, but scraped and converted from HTML into a readable format (for humans and LLMs) returned as chunks.

Useful because both the AgentFlow project and the Infer project depend on scraping webpages for RAG.

TODO: package as a Docker container.

## embedding-server

A simple Python server that exposes an endpoint `/embeddings` for getting text embeddings.

Useful because both AgentFlow and Infer services depend on getting embeddings.

## infer

This is a proxy for LLM inference services, such as VLLM, text-generation-inference (or any OpenAI API compatible service), but adds some useful things on top:

* A function calling convention (trivial but working)
* Template-based infilling, inspired by Guidance (much more trivial than Guidance but very useful)

This service adds those things and then re-exposes itself using the OpenAI API.

This is written in Rust.

## chat-ui

This is Huggingface's project chat-ui, with a slightly modified Dockerfile. Can be used as a web-based chat experience that targets the Infer service.

## text-generation-inference

This is Huggingface's project text-generation-inference. Its responsibility is to host the model and expose an API for inference that is super robust, super fast, super efficient, and super easy to set up. It is similar to vllm, but exposes a slightly different API.

## webprompt

TODO

## results of various model hosts

tgi + phi-3-medium-hf + eetq:
- loads, fast, but bad outputs / bad function calling?

tgi + phi-3-medium-hf + fp8:
- loads, fast, but bad outputs / bad function calling?
- omg figured it out.... :/ it's the Phi-3 prompt format that skips system prompts (I think)

must set prompt format to:
{% for message in messages %}
  {{'<|' + message['role'] + '|>\n' + message['content'] + '<|end|>\n' }}
{% endfor %}
{% if add_generation_prompt %}
    {{ '<|assistant|>\n' }}
{% endif %}

{% for message in messages %}{{'<|' + message['role'] + '|>\n' + message['content'] + '<|end|>\n' }}{% endfor %}{% if add_generation_prompt %}{{ '<|assistant|>\n' }}{% endif %}