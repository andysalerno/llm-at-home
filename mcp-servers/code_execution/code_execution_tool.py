from dataclasses import dataclass
from mcp.server.fastmcp import FastMCP
import logging
import os

CODE_EXECUTION_ENDPOINT = os.getenv(
    "CODE_EXECUTION_ENDPOINT", "http://localhost:8003/execute"
)


def setup_mcp(mcp: FastMCP):
    logger = logging.getLogger(__name__)

    @mcp.tool()
    async def execute_python_code(code: str) -> str:
        """
        Executes Python code and returns the output from stdout.
        Multiple lines are allowed, including complex scripting.
        IMPORTANT: you ONLY see the results from stdout, so you MUST print() the result you want to see.
        Simply placing the result on the last line will not work.

        Args:
            code: The Python code to execute, which must include a print statement to output the result.
        """
        logger.info("Executing code using endpoint: %s", CODE_EXECUTION_ENDPOINT)
        return await CodeExecutionTool(CODE_EXECUTION_ENDPOINT)(code)


@dataclass
class CodeExecutionTool:
    endpoint: str

    async def __call__(self, code: str) -> str:
        import httpx

        if "print(" not in code:
            return "Your code does not appear to print anything! Make sure to print the result you want to see."

        if self.endpoint.startswith("http://"):
            endpoint = self.endpoint
        else:
            endpoint = f"http://{self.endpoint}"

        # in case the model added markdown wrapper to the code, remove it:
        code = code.replace("```python\n", "").replace("\n```", "").strip()

        async with httpx.AsyncClient() as client:
            response = await client.post(endpoint, json={"code": code})
            if response.status_code != 200:
                return f"Code execution failed: {response.text}"
            return response.text


async def _serve(mcp: FastMCP):
    # await mcp.run_sse_async()
    await mcp.run_streamable_http_async()


if __name__ == "__main__":
    import asyncio

    mcp = FastMCP("execute_python_code")
    setup_mcp(mcp)
    asyncio.run(_serve(mcp))
