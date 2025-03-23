default:
  just --list

run-telemetry:
  uv --directory ./telemetry run python -m phoenix.server.main serve