from pydantic_ai import Agent, RunContext
from pydantic_ai.models import Model
from pydantic_ai.usage import UsageLimits


def run_example(model: Model):
    joke_selection_agent = Agent(
        model,
        system_prompt=(
            "Use the `joke_factory` to generate some jokes, then choose the best. "
            "You must return just a single joke."
        ),
    )
    joke_generation_agent = Agent(model, result_type=list[str])

    @joke_selection_agent.tool
    async def joke_factory(ctx: RunContext[None], count: int) -> list[str]:
        r = await joke_generation_agent.run(
            f"Please generate {count} jokes.",
            usage=ctx.usage,
        )
        return r.data

    result = joke_selection_agent.run_sync(
        "Tell me a joke.",
        usage_limits=UsageLimits(request_limit=5, total_tokens_limit=300),
    )
    print(result.data)
    print(result.usage())
