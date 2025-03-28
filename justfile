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
  uv --directory ./pydantic-server run --env-file .env main.py

run-langgraph:
  uv --directory ./langgraph-server run --env-file .env main.py

run-vibebot:
  uv --directory ./vibebot run --env-file .env main.py