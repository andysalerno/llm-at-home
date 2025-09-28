from agents import TResponseInputItem

from config import config

# remove previous tool calls from context, EXCEPT handoff messages (transfer_to_* calls)

TRIM_LEN = 1024


def trim_tool_calls(input: list[TResponseInputItem]) -> list[TResponseInputItem]:
    result = []

    skip_allowed = True

    for item in input:
        if item.get("type") == "function_call_output":
            if config.REMOVE_OLD_TOOL_CALLS and skip_allowed:
                continue

            skip_allowed = True

            prev = item.get("output")
            if not isinstance(prev, str):
                continue

            if len(prev) > TRIM_LEN:
                # If the output is too long, trim it to the first 128 characters
                item["output"] = prev[:TRIM_LEN] + "...(truncated for brevity)"
            else:
                item["output"] = prev

            result.append(item)
        elif item.get("type") == "function_call":
            if config.REMOVE_OLD_TOOL_CALLS:
                # just skip it entirely
                if item.get("name").startswith("transfer_to"):
                    skip_allowed = False
                    result.append(item)
                continue

            # redacting old tool call names may help the model understand it cannot call all old tools?
            # item["name"] = "redacted_function_name"
            result.append(item)
        elif item.get("type") == "message":
            content = item.get("content")
            content = content[0]  # type: ignore

            if content["type"] == "output_text" and (
                len(content["text"]) == 0 or content["text"].isspace()
            ):
                continue

            result.append(item)

        # elif item.get("type") == "reasoning":
        #     continue
        else:
            result.append(item)

    return result
