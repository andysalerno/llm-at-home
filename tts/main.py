import torchaudio as ta
from chatterbox.tts import ChatterboxTTS

model = ChatterboxTTS.from_pretrained(device="cuda")

text = "Alright, alright, settle in, Beatles fans! We've got a real gem for you today, a track that's often overlooked but packs a serious punch. This one was born out of a request for another song for the 'Yellow Submarine' soundtrack, and it certainly delivered! It's got that raw, energetic feel, and you can practically hear the fun they were having in the studio. In fact, John and Paul even finished writing it right there on the spot! And listen closely at the end for some playful barks - that's Paul trying to make John laugh, and it stuck! This is Hey Bulldog!"
# wav = model.generate(text)
# ta.save("test-1.wav", wav, model.sr)

AUDIO_PROMPT_PATH = "reference2.wav"
wav = model.generate(text, audio_prompt_path=AUDIO_PROMPT_PATH)
ta.save("outputs/test-1.wav", wav, model.sr)
