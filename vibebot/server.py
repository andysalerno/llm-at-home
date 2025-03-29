from flask import Flask, request, jsonify
from pydantic import BaseModel
from typing import Optional


# Define a strongly-typed request model
class ApiRequest(BaseModel):
    message: str
    user_id: Optional[str] = None
    timestamp: Optional[str] = None


app = Flask(__name__)


@app.route("/api", methods=["POST"])
def handle_request():
    # Parse and validate the request body using the Pydantic model
    try:
        data = ApiRequest(**request.get_json())
        # Return a simple JSON response
        return jsonify({"result": "ok", "message_received": data.message})
    except Exception as e:
        return jsonify({"result": "error", "error": str(e)}), 400


if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000, debug=True)
