"""
Data loader module for parsing items from a JSON file.

This module provides functionality to load and parse item data using Pydantic models.
"""

import json
import logging
from datetime import datetime
from pathlib import Path

from pydantic import BaseModel, Field, HttpUrl

logger = logging.getLogger(__name__)


class WikiItem(BaseModel):
    """Model representing a WikiData item."""

    item: HttpUrl = Field(description="Wikidata entity URL")
    item_label: str = Field(
        alias="itemLabel", description="Label of the item (e.g., person's name)"
    )
    en_article: HttpUrl = Field(
        alias="enArticle", description="URL to the English Wikipedia article"
    )
    dob: datetime | None = Field(None, description="Date of birth")
    dod: datetime | None = Field(None, description="Date of death")
    sitelinks: int = Field(description="Number of sitelinks")

    model_config = {
        "populate_by_name": True,
        "arbitrary_types_allowed": True,
    }


class DataLoader:
    """Loads and processes data from JSON files."""

    @staticmethod
    def load_items(file_path: str | Path) -> list[WikiItem]:
        """
        Load items from a JSON file.

        Args:
            file_path: Path to the JSON file containing items.

        Returns:
            List of WikiItem objects parsed from the JSON file.

        Raises:
            FileNotFoundError: If the specified file doesn't exist.
            json.JSONDecodeError: If the file is not valid JSON.
        """
        path = Path(file_path)
        if not path.exists():
            error_msg = f"File not found: {file_path}"
            raise FileNotFoundError(error_msg)

        with path.open("r", encoding="utf-8") as f:
            data = json.load(f)

        return [WikiItem.model_validate(item) for item in data]


# Example usage
if __name__ == "__main__":
    # Configure logging
    logging.basicConfig(
        level=logging.INFO, format="%(asctime)s - %(levelname)s - %(message)s"
    )

    # This is just for demonstration and will only run if this file is executed directly
    try:
        items = DataLoader.load_items("sample_data.json")
        logger.info("Loaded %d items", len(items))

        # Log details of the first item if any were loaded
        if items:
            item_json = items[0].model_dump_json(indent=2)
            logger.info("First item: %s", item_json)
    except (FileNotFoundError, json.JSONDecodeError):
        logger.exception("Error loading data")
