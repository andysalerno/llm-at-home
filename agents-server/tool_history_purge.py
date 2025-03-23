from smolagents import ActionStep, CodeAgent


def update_screenshot(memory_step: ActionStep, agent: CodeAgent) -> None:
    latest_step = memory_step.step_number
    for (
        previous_memory_step
    ) in agent.memory.steps:  # Remove previous tool calls from logs for lean processing
        if (
            isinstance(previous_memory_step, ActionStep)
            and previous_memory_step.step_number is not None
            and latest_step is not None
            and previous_memory_step.step_number < latest_step - 1
        ):
            previous_memory_step.tool_calls = None
