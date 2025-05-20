import logging

from pydantic import BaseModel
from pydantic_ai import Agent
from pydantic_ai.models import Model

_logger = logging.getLogger(__name__)

# canned intro, then...


class _Response(BaseModel):
    quote: str


def generate_1(model: Model, person_name: str, wiki_excerpt: str):
    agent = Agent(model, output_type=_Response, instructions=_prompt_1)

    response = agent.run_sync(_create_user_message(person_name, wiki_excerpt))


def _create_user_message(person_name: str, wiki_excerpt: str):
    return f"- {person_name} might say:"


_prompt_1 = """
Imagine a notable person has just emerged from a time machine into the present day.

They would something to the effect of: 'Where am I?! What's going on?!'

But, not all notable people would say the exact same thing.

Some examples:
- Einstein might say: 'Where am I? What in the world is going on? I was just giving a lecture!'
- John Lennon might say: 'Fucking hell!! Paul and I were just working in the studio! Where am I?'
- Winston Churchill might say: 'What the bloody hell is going on? Where are my generals? And have you got a cigar?'
- Marie Curie might say: 'Where am I? What is this strange place? I was just in my lab working on my research!'
""".strip()
