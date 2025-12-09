from logging import Logger, getLogger
from typing import Any

from agents import RunContextWrapper, RunHooks
from agents.agent import Agent
from agents.tool import Tool


class Output:
    def __init__(self) -> None:
        self._logger = getLogger("agentscli")

    def message(self, text: str) -> None:
        print(text)

    def streaming_message(self, text: str) -> None:
        print(text, end="", flush=True)

    def capture_user_input(self) -> str:
        return input("\n> ")

    def logger(self, name: str) -> Logger:
        return getLogger(f"agentscli.{name}")


class LoggingAgentRunHooks(RunHooks):
    def __init__(self, output: Output) -> None:
        super().__init__()
        self._output = output

    def on_tool_start(
        self,
        context: RunContextWrapper[Any],
        agent: Agent[Any],
        tool: Tool,
    ) -> None:
        self._output.logger("agent_hooks").info(
            f"[Invoking tool: {context.tool_name}({context.tool_arguments})]",
        )
        return super().on_tool_start(context, agent, tool)
