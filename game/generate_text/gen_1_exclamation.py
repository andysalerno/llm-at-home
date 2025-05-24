import logging

from pydantic import BaseModel
from pydantic_ai import Agent
from pydantic_ai.models import Model

_logger = logging.getLogger(__name__)


class _Response(BaseModel):
    quote: str


def generate_1(model: Model, person_name: str, wiki_excerpt: str) -> str:
    agent = Agent(model, output_type=_Response, instructions=_prompt_1)

    response = agent.run_sync(_create_user_message_1(person_name, wiki_excerpt))

    return response.output.quote


def _create_user_message_1(person_name: str, wiki_excerpt: str) -> str:
    wiki_excerpt = (
        "<WIKIPEDIA_EXCERPT>\n" + wiki_excerpt.strip() + "\n</WIKIPEDIA_EXCERPT>"
    )

    return f"{wiki_excerpt}\n\n- {person_name} might say:"


def generate_2(model: Model, person_name: str, wiki_excerpt: str) -> str:
    agent = Agent(model, output_type=_Response, instructions=_prompt_2)

    response = agent.run_sync(_create_user_message_2(person_name, wiki_excerpt))

    return response.output.quote


def _create_user_message_2(person_name: str, wiki_excerpt: str) -> str:
    wiki_excerpt = (
        "<WIKIPEDIA_EXCERPT>\n" + wiki_excerpt.strip() + "\n</WIKIPEDIA_EXCERPT>"
    )

    return f"{wiki_excerpt}\n\n- {person_name} might say:"


def generate_4(model: Model, person_name: str, wiki_excerpt: str) -> str:
    agent = Agent(model, output_type=_Response, instructions=_prompt_4_b)

    response = agent.run_sync(_create_user_message_4(person_name, wiki_excerpt))

    return response.output.quote


_prompt_1 = """
Imagine a notable person has just emerged from a time machine into the present day.

They would something to the effect of: 'Where am I?! What's going on?!'

But, not all notable people would say the exact same thing.

Some examples:
- Einstein might say: 'Where am I? What in the world is going on? I was just having a debate with Niels Bohr, when I heard a loud bang and then I was here!'
- John Lennon might say: 'Fucking hell!! Paul and I were just working in the studio, and then I heard a loud ruckus and then I was here!'
- Mahatma Gandhi might say: 'Where am I? What is going on? I was just meditating on the nature of satyagraha, when I felt a great disturbance and then I was here!'
- Leo Tolstoy might say: 'Good heavens! Where am I? What year is it? I was just discussing matters of the soul with my disciples, and then I heard a most peculiar noise and suddenly I am here!'
""".strip()

_prompt_2 = """
Imagine a notable person has just emerged from a time machine into the present day. They don't know what's going on or who you are.

They see you staring at them.

Noticing this, they speak to you, indignant, saying something like: "Quit staring and tell me what's going on!"

But, not all notable people would say the exact same thing.

Some examples:
- Einstein might say: 'Well, quit staring! What manner of spooky action at a distance is this?'
- Charles Darwin might say: 'What is the meaning of this? Explain yourself at once! Have I stumbled into some new Galapagos, or is this Bedlam?'
""".strip()

_prompt_4 = """
Imagine a notable person has just emerged from a time machine into the present day.

The tone of this story is: VISUAL NOVEL / DATING SIMULATOR

As such: when the person arrives, they want to be sent back - but, right before they are sent back, they see you staring, and feel a sudden attraction to you.

An awkward silence ensues.

To break the awkward silence, they speak, to say something along the lines of:
'Perhaps I could stay for a bit... <some lame excuse about why, when the real reason is that they are attracted to you>'

But, not all notable people would say the exact same thing.

Some examples:
- Einstein might say: 'Well... perhaps it would be best if I stayed just for a bit... it would be really good for me... for my research, I mean...'
- Charles Darwin might say: 'Well... perhaps I could stay for just a bit... it might be good for me... to observe the progression of evolution, I mean...'
- Nikola Tesla might say: 'Well... perhaps I could linger a bit... I want to know more about this electricity I feel...'
- Elvis Presley might say: 'Ya know... perhaps I could stay a bit longer, doll... I want to know more about... your music...'
- Margaret Thatcher might say: 'Well... perhaps I could stay a bit longer... there are a few things I'd like to... privatize...'
""".strip()

_prompt_4_b = """
fill in a template like the below
> be me
> scientist
> time machine finally works
> about to go back in time to meet some cuties kek
> accidentally press the wrong button
> Albert Einstein appears in front of me
> they look confused
> I stare at them, they stare at me
> they're kinda cute tho
> awkward silence
> you're about to send them back
> they blush and say 'Perhaps I could stay for a bit... to learn more about this attraction... gravitational attraction, I mean...'

> be me
> scientist
> time machine finally works
> about to go back in time to meet some cuties kek
> accidentally press the wrong button
> Margaret Thatcher appears in front of me
> they look confused
> I stare at them, they stare at me
> they're kinda cute tho
> awkward silence
> you're about to send them back
> they blush and say 'Maybe I could stay for a bit... there are some things I'd like to... privatize...'

> be me
> scientist
> time machine finally works
> about to go back in time to meet some cuties kek
> accidentally press the wrong button
> Winston Churchill appears in front of me
> they look confused
> I stare at them, they stare at me
> they're kinda cute tho
> awkward silence
> you're about to send them back
> they blush and say 'Perhaps I could stay for a bit... I have a great fondness for history, and I find myself quite... interested in yours...'

> be me
> scientist
> time machine finally works
> about to go back in time to meet some cuties kek
> accidentally press the wrong button
> Karl Marx appears in front of me
> they look confused
> I stare at them, they stare at me
> they're kinda cute tho
> awkward silence
> you're about to send them back
> they blush and say 'Perhaps I could stay for a bit... There's something here I'd like to seize... the means of production, I mean...'
""".strip()


def _create_user_message_4(person_name: str, wiki_excerpt: str) -> str:
    wiki_excerpt = (
        "<WIKIPEDIA_EXCERPT>\n" + wiki_excerpt.strip() + "\n</WIKIPEDIA_EXCERPT>"
    )

    context = f"<CONTEXT>\nsetting: laboratory. present individuals: only you and {person_name}\n</CONTEXT>"

    prompt = f"""
> be me
> scientist
> time machine finally works
> about to go back in time to meet some cuties kek
> accidentally press the wrong button
> {person_name} appears in front of me
> they look confused
> I stare at them, they stare at me
> they're kinda cute tho
> awkward silence
> you're about to send them back
> they blush and say '<quote>'
    """.strip()

    return f"{wiki_excerpt}\n\n{context}\n\n{prompt}"
