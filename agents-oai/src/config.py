"""Centralized configuration module for environment variables."""

import os


class Config:
    """Centralized configuration class that handles all environment variables."""

    # Agent behavior settings
    @property
    def USE_HANDOFFS(self) -> bool:
        return os.getenv("USE_HANDOFFS", "true").lower() in ("true", "1", "yes")

    @property
    def USE_HANDOFFS_PROMPT(self) -> bool:
        return os.getenv("USE_HANDOFFS_PROMPT", "true").lower() in ("true", "1", "yes")

    @property
    def PARALLEL_TOOL_CALLS(self) -> bool:
        return os.getenv("PARALLEL_TOOL_CALLS", "true").lower() in ("true", "1", "yes")

    @property
    def ENABLE_REASON_TOOL(self) -> bool:
        return os.getenv("ENABLE_REASON_TOOL", "false").lower() in ("true", "1", "yes")

    # Agent parameters
    @property
    def RESPONDING_AGENT_TEMP(self) -> float:
        return float(os.getenv("RESPONDING_AGENT_TEMP", "0.2"))

    @property
    def RESPONDING_AGENT_TOP_P(self) -> float:
        return float(os.getenv("RESPONDING_AGENT_TOP_P", "0.75"))

    @property
    def MAX_TURNS(self) -> int:
        return int(os.getenv("MAX_TURNS", "25"))

    # Model configuration
    @property
    def MODEL_NAME(self) -> str | None:
        return os.getenv("MODEL_NAME")

    @property
    def MODEL_BASE_URI(self) -> str | None:
        return os.getenv("MODEL_BASE_URI")

    @property
    def MODEL_API_KEY(self) -> str | None:
        return os.getenv("MODEL_API_KEY")

    # Context management
    @property
    def REMOVE_OLD_TOOL_CALLS(self) -> bool:
        return os.getenv("REMOVE_OLD_TOOL_CALLS", "true").lower() in (
            "true",
            "1",
            "yes",
        )


# Create a singleton instance for easy importing
config = Config()
