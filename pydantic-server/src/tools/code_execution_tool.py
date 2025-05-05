from dataclasses import dataclass

from pydantic_ai import ModelRetry, Tool


def create_code_execution_tool(
    host: str = "http://localhost",
    port: int = 8003,
    description: str | None = None,
    name: str | None = None,
) -> Tool:
    name = name or "execute_python_code"
    description = description or (
        "Executes Python code and returns the output from stdout. "
        "Multiple lines are allowed, including complex scripting. "
        "IMPORTANT: you ONLY see the results from stdout, so you MUST print() the result you want to see. "
        "Simply placing the result on the last line will not work."
    )
    return Tool(
        name=name,
        description=description,
        function=CodeExecutionTool(host, port).__call__,
        takes_ctx=False,
    )


@dataclass
class CodeExecutionTool:
    host: str
    port: int

    async def __call__(self, code: str) -> str:
        """
        Executes the given python code and returns the result.

        Args:
            code: The code to execute. May contain multiple lines, including complex scripting.

        Returns:
            The result of the code execution. If the code fails to build, the error message is returned.
        """
        import httpx

        if "print(" not in code:
            raise ModelRetry(
                "Your code does not appear to print anything! Make sure to print the result you want to see."
            )

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
                raise ModelRetry("Code execution failed: %s", response.text)  # type: ignore
            return response.text
