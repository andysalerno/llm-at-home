# AI Assistant Project Instructions

Concise, project-specific guidance for coding agents. Focus on actionable patterns in this monorepo (Rust + Python + Docker) powering local LLM + tool calling.

## 1. Big Picture
- Monorepo goal: self‑hostable ChatGPT‑like stack: model hosting (vLLM / TGI), function/tool calling, agents in multiple runtimes, emb + scraping + MCP tool servers, tracing via Phoenix.
- Two core architectural styles:
  1. Python agent stacks (`agents-*`, `mcp-servers`, `embedding-server`, `tts-server`) using uv for env mgmt and OpenAI/LiteLLM compatible models.
  2. Rust graph / agent execution (`agents-graphs`, `graphs-*`, `openai-model`, `scraper`) composing Actions over state for deterministic orchestration.
- Interop via HTTP (OpenAI-compatible chat/completions, MCP streamable HTTP at /mcp, custom embedding /embeddings, Phoenix OTLP on 4317/8008).

## 2. Core Services & Folders
- `agents-oai`: Streaming CLI chat using custom Agents library + MCP tools. Entry: `src/main.py`, orchestration: `manager.py`, model bootstrap: `model.py` (wraps `LitellmModel`).
- `agents-pydantic`: Pydantic-AI agent with memory tools; instrumentation pattern (`_configure_phoenix`). Shows tool injection with `extra_tools` and state mgmt in `state.py`.
- `agents-smolagents` / `agents-langgraph`: Alternative frameworks (not deeply instrumented yet) – follow `justfile` targets for run commands.
- `agents-graphs/graphs-ai`: Rust agent node pattern (`agent.rs`) building `Action<ConversationState>` with tool descriptors passed to model request.
- `mcp-servers`: Aggregated MCP server exposing search, browsing, code exec, wiki. Main: `server.py` calling `FastMCP.run_streamable_http_async()`.
- `embedding-server`: `/embeddings` endpoint (Python) likely used for vector search / RAG (Chromadb in `compose.yaml`).
- `mcp-server-backends/sandboxed-code-execution`: Isolated code execution HTTP service consumed by MCP.
- `compose*.yaml`: Launch stacks (core services `compose.yaml`; model hosting `compose.vllm.yaml`). Use as documentation for ports/env.
- `justfile`: Canonical dev workflow commands (prefer invoking these vs duplicating logic).

## 3. Execution & Dev Workflows
- Python agents: use uv with explicit directory flag and `.env` loading (pattern: `uv --directory ./agents-<name> run --env-file .env <entry>`). Replicate for new agent dirs.
- Streaming model responses: In `agents-oai/manager.py`, iterate `result.stream_events()`; handle event types `raw_response_event` with subtypes `response.output_text.delta`, tool invocation logging on `function_call` items, and agent updates. Preserve this event taxonomy when extending streaming logic.
- Model initialization: Always call `initialize_model()` once (guarded). For multi-model support, extend `LitellmModel` or add a factory; don't reassign global `MODEL`.
- Phoenix/OpenTelemetry instrumentation: See `_configure_phoenix()` in `agents-pydantic/src/main.py` (sets OTLP HTTP exporter to `http://localhost:8008`). Follow that pattern to instrument new agents.
- Rust graphs: Build & run with `cargo run` inside `agents-graphs` (wrapped by `just run-graphs`). Create new nodes with `Action::new(name, Box::new(|state| { ... state.with_added_message(...) }))`. Tool descriptors passed in vector at construction.

## 4. Tool / MCP Integration
- MCP client usage: `agents-oai/src/main.py` opens `MCPServerStreamableHttp` with params `{url: http://localhost:8002/mcp}` and `cache_tools_list=True`. Reuse this context manager when adding agents needing tool calls.
- MCP server assembly: In `mcp-servers/server.py`, each tool module has a `setup_mcp(mcp)` function. New tools should follow that module pattern and be added in `setup_mcp`.
- Function/tool calling at model layer: Rust side passes `tools` (Vec<ToolDescription>) into `ChatCompletionRequest::new(..., Some(tools.into_iter().map(|t| t.into()).collect()), ...)`. Mirror this structure for additional model features.

## 5. Environment & Config Conventions
- Env vars for model selection in `agents-oai`: `MODEL_NAME`, `MODEL_BASE_URI`, `MODEL_API_KEY`; prompt user if missing unless base URL is localhost. Keep same names to maintain UX.
- Ports (from compose): Phoenix 8008, Chroma 8001->8000, Scraper 3000, Code Exec 8003, MCP server 8002, Model host (vLLM) 8000.
- GPU model host (vLLM): Use `compose.vllm.yaml` comments as canonical reference for model variants & flags (e.g., `--tool-call-parser hermes`, `--enable-auto-tool-choice`). When adding new models, document flags inline as comment examples to retain this living notebook style.

## 6. Adding New Components (Patterns)
- New Python agent: create `agents-<name>/pyproject.toml`, `src/main.py` mirroring env + instrumentation, add run target to `justfile` with uv directory invocation.
- New MCP tool: module with `setup_mcp(mcp: FastMCP)` that registers functions; import and call in central `setup_mcp` in `mcp-servers/server.py`.
- New Rust action node: implement function returning `Action<ConversationState>`; keep side effects minimal and return updated state (immutability pattern).
- Streaming UX extension: add new event type handlers adjacent to existing ones; avoid blocking operations inside the `async for event` loop.

## 7. Testing & Validation (Current Reality)
- No centralized test harness present; rely on manual CLI runs (`just run-oai`, etc.) and observing Phoenix traces. If adding tests, colocate near component using framework conventions; keep them optional to not block existing flows.

## 8. Style & Guardrails
- Prefer small, composable modules; follow existing naming (`create_responding_agent`, `initialize_model`).
- Avoid global mutable state except the singular `MODEL` pattern; if extended, wrap in accessor functions.
- Keep README updates minimal—empty component READMEs indicate WIP; focus on in-code docs & this file for guidance.

## 9. Quick Start Snippets
- Start core tool stack: `docker compose up` (uses `compose.yaml`).
- Start vLLM host: `docker compose -f compose.vllm.yaml up` (or `just vllm-up`).
- Run OAI agent CLI: `just run-oai` (prompts for model/env if not set).
- Run MCP server standalone: `uv --directory mcp-servers run --env-file ./google_search/.env server.py`.

## 10. When Unsure
- Inspect `justfile` first for canonical commands.
- Trace data flow: input -> CLI loop -> `run_single` -> streaming events -> model/tool calls via MCP.
- For model/tool schema mismatches, mirror existing tool call logging approach before refactoring.

(End)
