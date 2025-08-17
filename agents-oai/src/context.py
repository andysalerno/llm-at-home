from agents import TResponseInputItem
from openai.types.responses.response_input_item_param import FunctionCallOutput


def trim_tool_calls(input: list[TResponseInputItem]) -> list[TResponseInputItem]:
    result = []

    for item in input:
        if item.get("type") == "function_call_output":
            prev = item.get("output", "")
            if not isinstance(prev, str):
                continue

            item["output"] = prev[:128]

            result.append(item)
        else:
            result.append(item)

    return result
