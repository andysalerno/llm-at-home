set export

PHOENIX_PORT := "8007"

default:
  just --list

run-telemetry:
  uv --directory ./telemetry run python -m phoenix.server.main serve

run-agents:
  uv --directory ./agents-server pip install "smolagents @ ./smolagents"
  uv --directory ./agents-server run main.py

run-agents-local:
  uv --directory ./agents-server-local run main.py

run-pydantic:
  uv --directory ./pydantic-server run --env-file .env src/main.py

run-langgraph:
  uv --directory ./langgraph-server run --env-file .env src/main.py

[working-directory: 'sandboxed-code-execution']
run-code-sandbox:
  docker run -p 8003:8003 -e PORT=8003 python-sandbox 
