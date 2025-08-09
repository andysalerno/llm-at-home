from pydantic import BaseModel

from agents import Agent
from model import get_model

PROMPT = (
    "You are a helpful research assistant. Given a query, come up with a set of web searches "
    "to perform to best answer the query. Output between 5 and 20 terms to query for. "
    "Web searches must be provided as structured JSON, as a list of object where each object has the query and the reason for the search."
)


class WebSearchItem(BaseModel):
    reason: str
    "Your reasoning for why this search is important to the query."

    query: str
    "The search term to use for the web search."


class WebSearchPlan(BaseModel):
    searches: list[WebSearchItem]
    """A list of web searches to perform to best answer the query."""


def create_planner_agent() -> Agent:
    return Agent(
        name="PlannerAgent",
        instructions=PROMPT,
        model=get_model(),
        output_type=WebSearchPlan,
    )
