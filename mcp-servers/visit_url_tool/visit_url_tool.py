import logging
import urllib.parse
from dataclasses import dataclass
import httpx
from pydantic import BaseModel, ConfigDict, Field
from mcp.server.fastmcp import FastMCP
import os

logger = logging.getLogger(__name__)

SCRAPPER_ENDPOINT = os.getenv("SCRAPPER_ENDPOINT", "http://localhost:3000")
MAX_RESPONSE_LEN = int(os.getenv("MAX_RESPONSE_LEN", "8000"))


class ScrapperResponse(BaseModel):
    model_config = ConfigDict(extra="ignore")

    title: str
    content: str
    text_content: str = Field(alias="textContent")
    url: str


@dataclass
class ScrapperClient:
    """
    A client for Scrapper:
    https://github.com/amerkurev/scrapper.
    """

    scrapper_endpoint: str

    async def scrape(self, url: str) -> ScrapperResponse:
        logger.info("Visiting url: %s", {url})
        api_uri = self._create_api_uri(url)
        async with httpx.AsyncClient() as client:
            try:
                response = await client.get(api_uri)
                if response.is_error:
                    return ScrapperResponse(
                        title="Error",
                        content="Error",
                        textContent="The scraper returned an error.",
                        url="Error",
                    )
            except:
                return ScrapperResponse(
                    title="Error",
                    content="Error",
                    textContent="The scraper returned an error.",
                    url="Error",
                )

            return ScrapperResponse.model_validate_json(response.text)

    def _create_api_uri(self, url: str) -> str:
        if "(" in url and not ")" in url:
            logger.warning("fixing url which is missing closing paren")
            url = url + ")"

        url = url.strip()

        scrapper_endpoint = self.scrapper_endpoint.rstrip("/")
        encoded_url = urllib.parse.quote(url, safe="")
        return f"{scrapper_endpoint}/api/article?url={encoded_url}"


def setup_mcp(mcp: FastMCP):
    client = ScrapperClient(SCRAPPER_ENDPOINT)

    @mcp.tool(name="visit_url")
    async def scrape(url: str) -> str:
        """
        Visits (scrapes) the given URL and returns the text content of the page.
        Useful for looking up current events, or visiting urls you found in your research.

        Args:
            url: The url to visit (scrape).
        """
        response = await client.scrape(url)
        return response.text_content[:MAX_RESPONSE_LEN]


async def _serve(mcp: FastMCP):
    # await mcp.run_sse_async()
    await mcp.run_streamable_http_async()


if __name__ == "__main__":
    import asyncio

    mcp = FastMCP("visit_url")
    setup_mcp(mcp)
    asyncio.run(_serve(mcp))
