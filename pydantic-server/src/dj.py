import asyncio
import logging

from pydantic import BaseModel
from pydantic_ai import Agent
from pydantic_ai.mcp import MCPServerHTTP
from pydantic_ai.settings import ModelSettings

from model import create_model, get_extra_body

logger = logging.getLogger(__name__)


class Output(BaseModel):
    song_info: str


class Song(BaseModel):
    title: str


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
    agent = create_agent(
        temperature=0.4,
    )

    async with agent.run_mcp_servers():
        logger.info("Starting song loop...")

        songs = [Song(title="song: Hey Bulldog by The Beatles")]

        for song in songs:
            intro = await _get_intro_for_song(agent, song)

            logger.info("Intro for song '%s': %s", song.title, intro)
            # outro = _get_intro_for_song(agent, song)


async def _get_intro_for_song(agent: Agent[None, Output], song: Song) -> str:
    response = await agent.run(f"Song: {song.title}")
    logger.info("Response: %s", response)
    logger.info("Response: %s", response.all_messages_json())

    output = response.output
    return output.song_info


def _get_outro_for_song(agent: Agent[None, Output]) -> str:
    return "some outro"


def create_agent(temperature: float) -> Agent[None, Output]:
    _configure_phoenix()
    mcp_server = MCPServerHTTP(url="http://localhost:8000/sse")

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
    You are a radio DJ who specializes in The Beatles. You introduce songs and tell factual and interesting stories about them and their history.

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
    - "At the time this song was recorded, it was standard practice to edit out the count-in from the final pressing. But George Martin wanted the Beatles' opening song on their debut album to capture the energy of their live performances. Here it is, the opening track from Please Please Me, I Saw Her Standing There."

    """.strip()


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)

    # thought: with some probability (20%?) the intro prompt will also know about the previous song, to craft a kind of transition.
    asyncio.run(main())
