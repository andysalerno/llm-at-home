import os
import io
import contextlib
from flask import Flask, request, jsonify

app = Flask(__name__)

@app.route('/execute', methods=['POST'])
def execute_code():
    data = request.get_json()
    if not data or 'code' not in data:
        return jsonify({'error': 'No code provided'}), 400

    code = data['code']
    output = io.StringIO()

    # Use a simple restricted environment; you can adjust built-ins if needed
    safe_globals = {"__builtins__": __import__("builtins")}
    safe_locals = {}

    error = None
    try:
        # Redirect stdout and stderr so we capture all output
        with contextlib.redirect_stdout(output), contextlib.redirect_stderr(output):
            exec(code, safe_globals, safe_locals)
    except Exception as e:
        error = str(e)

    result = output.getvalue()
    response = {}
    if result:
        response['output'] = result
    if error:
        response['error'] = error

    return jsonify(response)

if __name__ == '__main__':
    port = int(os.environ.get("PORT", 5000))
    app.run(host='0.0.0.0', port=port)
