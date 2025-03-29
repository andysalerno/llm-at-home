from typing import Callable
from flask import Flask, request, jsonify
from pydantic import BaseModel
from pydantic_ai import Agent


class ApiRequest(BaseModel):
    chat_message: str


app = Flask(__name__)


def set_agent_provider(
    agent_provider: Callable[[], Agent],
):
    global _agent_provider
    _agent_provider = agent_provider


def get_agent_provider() -> Callable[[], Agent]:
    global _agent_provider
    if _agent_provider is None:
        raise ValueError("Agent provider is not set.")
    return _agent_provider


def set_user_message_builder(
    user_message_builder: Callable[[str], str],
):
    global _user_message_builder
    _user_message_builder = user_message_builder


def get_user_message_builder() -> Callable[[str], str]:
    global _user_message_builder
    if _user_message_builder is None:
        raise ValueError("User message builder is not set.")
    return _user_message_builder


@app.route("/api", methods=["POST"])
def handle_request():
    try:
        data = ApiRequest(**request.get_json())

        response = _get_agent_response(data.chat_message)

        return jsonify({"result": response})
    except Exception as e:
        return jsonify({"result": "error", "error": str(e)}), 400


def _get_agent_response(chat_message: str) -> str:
    agent = get_agent_provider()()

    user_message_builder = get_user_message_builder()
    message = user_message_builder(chat_message)

    response = agent.run_sync(message)

    return response.data


def run_server(
    agent_provider: Callable[[], Agent], user_message_builder: Callable[[str], str]
):
    set_agent_provider(agent_provider)
    set_user_message_builder(user_message_builder)
    app.run(host="0.0.0.0", port=5000, debug=False)
