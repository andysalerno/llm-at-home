"""Centralized configuration module for environment variables."""

import os
from typing import Optional


class Config:
    """Centralized configuration class that handles all environment variables."""

    # Agent behavior settings
    USE_HANDOFFS: bool = os.getenv("USE_HANDOFFS", "true").lower() in (
        "true",
        "1",
        "yes",
    )
    USE_HANDOFFS_PROMPT: bool = os.getenv("USE_HANDOFFS_PROMPT", "true").lower() in (
        "true",
        "1",
        "yes",
    )
    PARALLEL_TOOL_CALLS: bool = os.getenv("PARALLEL_TOOL_CALLS", "true").lower() in (
        "true",
        "1",
        "yes",
    )
    ENABLE_REASON_TOOL: bool = os.getenv("ENABLE_REASON_TOOL", "false").lower() in (
        "true",
        "1",
        "yes",
    )

    # Agent parameters
    RESPONDING_AGENT_TEMP: float = float(os.getenv("RESPONDING_AGENT_TEMP", "0.2"))
    RESPONDING_AGENT_TOP_P: float = float(os.getenv("RESPONDING_AGENT_TOP_P", "0.75"))
    MAX_TURNS: int = int(os.getenv("MAX_TURNS", "25"))

    # Model configuration
    MODEL_NAME: Optional[str] = os.getenv("MODEL_NAME")
    MODEL_BASE_URI: Optional[str] = os.getenv("MODEL_BASE_URI")
    MODEL_API_KEY: Optional[str] = os.getenv("MODEL_API_KEY")

    # Context management
    REMOVE_OLD_TOOL_CALLS: bool = os.getenv(
        "REMOVE_OLD_TOOL_CALLS", "true"
    ).lower() in ("true", "1", "yes")


# Create a singleton instance for easy importing
config = Config()
