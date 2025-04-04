from dataclasses import dataclass
from pydantic import BaseModel, ConfigDict, Field
from pydantic_ai import ModelRetry, Tool
import urllib.parse
import httpx


def create_visit_site_tool(
    scrapper_endpoint: str, max_response_len: int = 8000
) -> Tool[None]:
    client = ScrapperClient(scrapper_endpoint)

    async def scrape(url: str) -> str:
        """
        Args:
            url: The url to visit (scrape).
        """
        response = await client.scrape(url)
        return response.text_content[:max_response_len]

    tool = Tool(
        name="visit_site",
        description="Visits (scrapes) the given URL and returns the text content of the page.",
        function=scrape,
        takes_ctx=False,
    )

    return tool


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
    https://github.com/amerkurev/scrapper
    """

    scrapper_endpoint: str

    async def scrape(self, url: str) -> ScrapperResponse:
        api_uri = self._create_api_uri(url)
        async with httpx.AsyncClient() as client:
            response = await client.get(api_uri)
            if response.is_error:
                raise ModelRetry(
                    "scrapper returned an error, try fixing your request and retrying"
                )

            # parse as ScrapperResponse and return:
            print(f"saw json: {response.text}")
            scrapper_response = ScrapperResponse.model_validate_json(response.text)
            return scrapper_response

    def _create_api_uri(self, url: str) -> str:
        scrapper_endpoint = self.scrapper_endpoint.rstrip("/")
        # http encode the url:
        encoded_url = urllib.parse.quote(url, safe="")
        uri = f"{self.scrapper_endpoint}/api/article?url={encoded_url}"
        return uri
