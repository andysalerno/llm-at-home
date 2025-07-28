import asyncio
import logging

from agents.responding_assistant import create_responding_assistant
from chat_loop import run_loop
from state import State
from tools.memory_tool import search_memory_tool, store_memory_tool

logger = logging.getLogger(__name__)


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
    _configure_phoenix()

    agent = create_responding_assistant(
        temperature=0.4,
        extra_tools=[store_memory_tool(), search_memory_tool()],
        include_tools_in_prompt=False,  # change based on the model used
    )
    state = State()

    logger.info("Starting chat loop...")
    await run_loop(agent, state, trim_old_tool_outputs=True)


if __name__ == "__main__":
    # Configure logging to show only your code, not dependencies
    logging.basicConfig(level=logging.WARNING)  # Set high level for all loggers
    
    # Enable INFO level for your specific modules
    logging.getLogger(__name__).setLevel(logging.INFO)
    logging.getLogger("agents").setLevel(logging.INFO)
    logging.getLogger("chat_loop").setLevel(logging.INFO)
    logging.getLogger("state").setLevel(logging.INFO)
    logging.getLogger("tools").setLevel(logging.INFO)
    
    # Explicitly silence noisy dependencies
    logging.getLogger("httpx").setLevel(logging.WARNING)
    logging.getLogger("openai").setLevel(logging.WARNING)
    logging.getLogger("urllib3").setLevel(logging.WARNING)

    asyncio.run(main())
