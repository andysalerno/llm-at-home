default:
  just --list

run-telemetry:
  uv --directory ./telemetry run python -m phoenix.server.main serve

run-agents:
  uv --directory ./agents-server run --env-file .env  main.py

run-game:
  uv --directory ./game run --env-file .env  main.py

run-pydantic:
  uv --directory ./pydantic-server run --env-file .env src/main.py

run-langgraph:
  uv --directory ./langgraph-server run --env-file .env src/main.py

[working-directory: 'sandboxed-code-execution']
run-code-sandbox:
  docker run --rm -p 8003:8003 -e PORT=8003 python-sandbox 

run-web-scraper:
  docker run --rm -p 3000:3000 amerkurev/scrapper:latest

[working-directory: 'mcp-servers']
run-mcp-server:
  uv run --env-file ./google_search/.env server.py

[working-directory: 'graphs']
run-graphs:
  #!/usr/bin/env bash
  set -a
  source ./.env
  set +a
  cargo run
