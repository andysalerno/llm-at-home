set export

PHOENIX_PORT := "8007"

default:
  just --list

run-telemetry:
  uv --directory ./telemetry run python -m phoenix.server.main serve

run-agents:
  uv --directory ./agents-server run main.py