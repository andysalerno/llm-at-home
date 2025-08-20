default:
  just --list

run-smolagents:
  uv --directory ./agents-smolagents run --env-file .env  main.py

run-pydantic:
  uv --directory ./agents-pydantic run --env-file .env ./src/main.py

run-oai:
  uv --directory ./agents-oai run --env-file .env ./src/main.py

run-langgraph:
  uv --directory ./agents-langgraph run --env-file .env src/main.py

vllm-up:
  sudo podman compose -f compose.llamacpp.yaml up

vllm-down:
  sudo podman compose -f compose.llamacpp.yaml down

llama-up:
  sudo podman compose -f compose.vllm.yaml up

llama-down:
  sudo podman compose -f compose.vllm.yaml up

[working-directory: 'mcp-server-backends/sandboxed-code-execution']
run-code-sandbox:
  docker run --rm -p 8003:8003 -e PORT=8003 python-sandbox 

run-web-scraper:
  docker run --rm -p 3000:3000 amerkurev/scrapper:latest

[working-directory: 'mcp-servers']
run-mcp-server:
  uv run --env-file ./google_search/.env server.py

[working-directory: 'agents-graphs']
run-graphs:
  #!/usr/bin/env bash
  set -a
  source ./.env
  set +a
  cargo run
