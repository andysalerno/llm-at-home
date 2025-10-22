from agents.mcp import MCPServer

_instance: dict[str, MCPServer] = {}


def get_named_server(name: str) -> MCPServer:
    if name not in _instance:
        raise ValueError(f"No MCP server registered with name '{name}'")
    return _instance[name]


def register_named_server(name: str, server: MCPServer) -> None:
    if name in _instance:
        raise ValueError(f"MCP server already registered with name '{name}'")
    _instance[name] = server
