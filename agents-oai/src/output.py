from logging import Logger, getLogger


class Output:
    def __init__(self) -> None:
        self._logger = getLogger("agentscli")

    def message(self, text: str) -> None:
        print(text)

    def streaming_message(self, text: str) -> None:
        print(text, end="", flush=True)

    def capture_user_input(self) -> str:
        return input("> ")

    def logger(self, name: str) -> Logger:
        return getLogger(f"agentscli.{name}")
