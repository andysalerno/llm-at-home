from agents import Agent, HostedMCPTool, WebSearchTool, Runner
from agents.mcp import MCPServerStreamableHttp, MCPServer
from agents.model_settings import ModelSettings
from model import get_model

INSTRUCTIONS = (
    "You are a research assistant. Given a search term, you search the web for that term and "
    "produce a concise summary of the results. The summary must be 2-3 paragraphs and less than 300 "
    "words. Capture the main points. Write succinctly, no need to have complete sentences or good "
    "grammar. This will be consumed by someone synthesizing a report, so its vital you capture the "
    "essence and ignore any fluff. Do not include any additional commentary other than the summary "
    "itself."
)


async def create_mcp_server() -> MCPServer:
    # server = MCPServerSse(params={"url": "http://localhost:8002/sse"})
    server = MCPServerStreamableHttp(params={"url": "http://localhost:8002/mcp"})
    await server.connect()

    return server


async def create_search_agent() -> Agent:
    """Create a search agent with the specified model and settings."""
    return Agent(
        name="Search agent",
        instructions=INSTRUCTIONS,
        mcp_servers=[await create_mcp_server()],
        model_settings=ModelSettings(tool_choice="required"),
        model=get_model(),
    )
