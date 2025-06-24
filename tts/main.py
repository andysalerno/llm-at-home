import os
import torchaudio as ta
from chatterbox.tts import ChatterboxTTS
from flask import Flask, request, jsonify

# Initialize the TTS model
model = ChatterboxTTS.from_pretrained(device="cuda")

# Create outputs directory if it doesn't exist
os.makedirs("outputs", exist_ok=True)

# Initialize Flask app
app = Flask(__name__)

AUDIO_PROMPT_PATH = "reference2.wav"


@app.route("/generate", methods=["POST"])
def generate_audio():
    try:
        # Get JSON data from request
        data = request.get_json()

        if not data:
            return jsonify({"error": "No JSON data provided"}), 400

        # Extract text and title from request
        text = data.get("text")
        title = data.get("title")

        if not text:
            return jsonify({"error": "Text is required"}), 400

        if not title:
            return jsonify({"error": "Title is required"}), 400

        # Generate audio
        wav = model.generate(text, audio_prompt_path=AUDIO_PROMPT_PATH)

        # Create output filename
        output_filename = f"{title}.wav"
        output_path = os.path.join("outputs", output_filename)

        # Save the audio file
        ta.save(output_path, wav, model.sr)

        return jsonify(
            {
                "message": "Audio generated successfully",
                "filename": output_filename,
                "path": output_path,
            }
        ), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/health", methods=["GET"])
def health_check():
    return jsonify({"status": "healthy"}), 200


if __name__ == "__main__":
    print("Starting TTS server...")
    print("POST to /generate with JSON: {'text': 'your text', 'title': 'filename'}")
    print("Health check available at /health")
    app.run(host="0.0.0.0", port=5000, debug=True)
