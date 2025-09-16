from agents import function_tool


@function_tool
async def reason(thinking: str) -> str:
    """Invoke to capture your reasoning / thought process / plan. Always invoke this tool before invoking any other tool to capture your reasoning.

    Args:
        thinking: The reasoning or thought process to be recorded.
    """
    return "(reasoning complete)"
