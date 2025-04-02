from dataclasses import dataclass
from pydantic_ai import ModelRetry, Tool


def create_code_execution_tool(
    host: str = "http://localhost", port: int = 8003
) -> Tool:
    return Tool(
        name="execute_python_code",
        description="Executes Python code and returns the result. IMPORTANT: you ONLY see the results from stdout, so you MUST print the result you want to see. Code MAY span multiple lines.",
        function=CodeExecutionTool(host, port).__call__,
    )


@dataclass
class CodeExecutionTool:
    host: str
    port: int

    async def __call__(self, code: str) -> str:
        """Executes the given python code and returns the result.

        Args:
            code: The code to execute.

        Returns:
            The result of the code execution. If the code fails to build, the error message is returned.
        """
        import httpx

        if self.host.startswith("http://"):
            base_url = self.host
        else:
            base_url = f"http://{self.host}"

        base_url = base_url.rstrip("/")
        if not self.port:
            raise ValueError("Port must be specified.")

        base_url = f"{base_url}:{self.port}"

        # in case the model added markdown wrapper to the code, remove it:
        code = code.replace("```python\n", "").replace("\n```", "").strip()

        async with httpx.AsyncClient(
            base_url=base_url,
        ) as client:
            response = await client.post("/execute", json={"code": code})
            if response.status_code != 200:
                raise ModelRetry(f"Code execution failed: {response.text}")
            return response.text
