# Monorepo Overview
This is a monorepo composed of small projects I have been working on related to self-hosting and serving LLMs.

These projects can combine to make a self-hostable, extensible, customizable, ChatGPT-like experience. It supports custom functions to extend the abilities of the model.

A Docker Compose file exists to easily launch the services, or to see how they can be manually started.

## agentflow (new)

This is a c# project which consists of a framework for building AI agents.

It has an examples dir that shows how agents may be built using the framework.

TODO: rewrite in Rust to go alongside the Infer library :)

TODO: package as a Docker container.

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