from smolagents import Tool
from duckduckgo_search import DDGS
import requests


class SearchAndScrape(Tool):
    name = "scrape_websites"
    description = "This tool performs a Google search for the given query, then visits the top 5 websites, scrapes them, and finally returns the most relevant chunks as a list of strings."
    inputs = {
        "search_query": {
            "type": "string",
            "description": "The Google search query",
        }
    }
    output_type = "string"

    def __init__(self, scraper_endpoint: str, scores_endpoint: str):
        self.scraper_endpoint = scraper_endpoint
        self.scores_endpoint = scores_endpoint
        self.ddgs = DDGS()
        super().__init__(self.name, self.description, self.inputs, self.output_type)

    def forward(self, search_query: str) -> str:
        # 1. Perform the search
        results = self.ddgs.text(search_query, max_results=5)
        if len(results) == 0:
            raise Exception("No results found! Try a less restrictive/shorter query.")

        # 2. scrape the top N websites using the scraper endpoint
        uris = [result["href"] for result in results]
        print(f"Scraping the following websites: {uris}")

        payload = {"uris": uris}

        # chunks is an array, where each item has the format
        # { "content": "...", "uri": "..." }
        chunks = []
        try:
            response = requests.post(self.scraper_endpoint, json=payload)

            print(f"Scraper response: {response.text}")

            response.raise_for_status()

            chunks = response.json().get("chunks", [])

            print(f"Scraper chunks: {chunks}")
        except requests.exceptions.RequestException as e:
            return f"Error while scraping websites: {str(e)}"

        # next, make a POST request to the scores endpoint
        # the payload format is:
        # { "input": ["array", "of", "string", "chunks"], "query": "query to score against (to find most relevanat chunks)" }
        # output is an array of floats, where each float corresponds to the score of the chunk at the same index
        scores: list[float] = []
        try:
            print("asking for scores...")
            response = requests.post(
                self.scores_endpoint,
                json={"input": chunks, "query": search_query},
            )

            response.raise_for_status()

            scores = response.json().get("scores", [])
        except requests.exceptions.RequestException as e:
            return f"Error while scoring chunks: {str(e)}"

        # finally, return the top N chunks by score:
        top_n = 3
        sorted_chunks = sorted(zip(chunks, scores), key=lambda x: x[1], reverse=True)
        top_chunks = [
            {"chunk": chunk, "score": score} for chunk, score in sorted_chunks[:top_n]
        ]

        # format the output as a string
        output = "\n=================\n\n".join(
            [
                f"[SCORE {round(chunk['score'], 3)}] {chunk['chunk']}"
                for chunk in top_chunks
            ]
        )

        return output
