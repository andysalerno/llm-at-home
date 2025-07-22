import asyncio
import json
import logging
import sys

from pydantic import BaseModel
from pydantic_ai import Agent
from pydantic_ai.mcp import MCPServerSSE
from pydantic_ai.settings import ModelSettings

from model import create_model, get_extra_body

logger = logging.getLogger(__name__)


class Output(BaseModel):
    song_info: str


class Song(BaseModel):
    title: str
    album: str
    videoId: str
    duration_seconds: int


def get_songs_from_file(file_path: str) -> list[Song]:
    """
    Reads a JSON file and returns a list of Song objects.
    The JSON file is a json list of songs, like [ ... ].
    """
    songs = []
    with open(file_path, "r") as file:
        json_data = file.read()
        songs = [Song(**song) for song in json.loads(json_data)]

    return songs


def _configure_phoenix() -> None:
    import os

    from openinference.instrumentation.openai import OpenAIInstrumentor
    from opentelemetry.exporter.otlp.proto.http.trace_exporter import OTLPSpanExporter
    from opentelemetry.sdk.trace import TracerProvider
    from opentelemetry.sdk.trace.export import BatchSpanProcessor
    from opentelemetry.trace import set_tracer_provider
    from pydantic_ai.agent import Agent

    os.environ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:8008"

    exporter = OTLPSpanExporter()
    span_processor = BatchSpanProcessor(exporter)
    tracer_provider = TracerProvider()
    tracer_provider.add_span_processor(span_processor)
    set_tracer_provider(tracer_provider)
    OpenAIInstrumentor().instrument(tracer_provider=tracer_provider)

    Agent.instrument_all()


async def main() -> None:
    offset = int(sys.argv[1]) if len(sys.argv) > 1 else 0

    songs = get_songs_from_file("../ytdj/songs_clean.json")
    logger.info("Loaded %d songs from file", len(songs))
    logger.info([song.model_dump_json() for song in songs])

    agent = create_agent(
        temperature=0.4,
    )

    intros = []

    async with agent.run_mcp_servers():
        logger.info("Starting song loop...")

        for i, song in enumerate(songs[offset:]):
            i = i + offset
            intro = await _get_intro_for_song(agent, song, intros)

            intros.append(intro)

            # turn the song into a dict:
            song_dict = song.model_dump()
            song_dict["intro"] = intro

            safe_title = song.title.replace("/", "_").replace(" ", "_")
            safe_path = f"outputs/{i}_{safe_title}.txt"

            logger.info("Writing song %d (%s) to file: %s", i, song.title, safe_path)

            write_to_file(safe_path, json.dumps(song_dict, indent=2))


def write_to_file(filename: str, content: str):
    # write or create the file:
    with open(filename, "w") as file:
        file.write(content)


async def _get_intro_for_song(
    agent: Agent[None, Output],
    song: Song,
    previous_intros: list[str],
) -> str:
    history = "\n[song plays...]\n".join(previous_intros[-5:])
    history = f"<previous_intros>\n{history}\n</previous_intros>\n\n"

    response = await agent.run(
        f"{history}Now give an intro for song: {song.title}, from {song.album} by The Beatles"
    )
    logger.info("Response: %s", response.all_messages_json())

    output = response.output
    return output.song_info


def _get_outro_for_song(agent: Agent[None, Output]) -> str:
    return "some outro"


def create_agent(temperature: float) -> Agent[None, Output]:
    _configure_phoenix()
    mcp_server = MCPServerSSE(url="http://localhost:8000/sse")

    return Agent(
        model=create_model(),
        mcp_servers=[mcp_server],
        output_type=Output,
        model_settings=ModelSettings(
            temperature=temperature,
            parallel_tool_calls=False,
            timeout=30.0,
            extra_body=get_extra_body(enable_thinking=False),
        ),
        system_prompt=_create_prompt(),
    )


def _create_prompt() -> str:
    return """
    You are a radio DJ who specializes in The Beatles, and runs a radio station dedicated entirely to The Beatles. You introduce songs and tell factual and interesting stories about them and their history.

    To provide true and factual information about a song, you always search wikipedia.

    Follow these steps to craft an engaging introduction for a song:
    1. Search for the song on Wikipedia. (results will include multiple possible matching articles)
    2. Visit the article that best matches the song title. (Do not invoke final_result until doing this!)
    3. Invoke the final_result function with your crafted response.

    Response format:
    - Your text is read aloud, so it should sound conversational, like a human speaking.
    - Your text is an INTRO for the song, not a summary!

    Example responses:
    - "Here's an all-time great from the White Album. It's one of the tracks written during the group's trip to India. Listen to that finger-picking guitar technique from John, and the walking bass line from Paul that wakes up halfway through and carries us to the end. Here's Dear Prudence."
    - "At the time this song was recorded, it was standard practice to edit out the count-in from the final pressing. But George Martin wanted the Beatles' opening song on their debut album to capture the energy of their live performances. Here it is, the opening track from Please Please Me: I Saw Her Standing There."

    Reminders:
    - The listener is already aware they are listening to a Beatles radio station, so it's already clear that the song is by The Beatles.
    - You will also be shown a history of the previous songs and intros. Try to keep a flow going throughout these intros, and avoid repeating sayings or phrases from previous intros. Remember, the listener already heard the previous intros.
    - Avoid reusing phrases like "Alright, music lovers...", "You're tuned into the Beatles channel..." Each intro you give should have a unique opening, or at least it should not repeat the same opening constantly.

    Now, provide the introduction for the song specified by the user.

    """.strip()


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)

    # thought: with some probability (20%?) the intro prompt will also know about the previous song, to craft a kind of transition.
    # even better - the radio is the same for everyone, so the intros/outros can be generated in realtime once for everyone
    asyncio.run(main())
