import logging

from data_loader import DataLoader
from generate_text.gen_1_exclamation import generate_1
from model import create_model
from wikipedia import get_wikipedia_summary

logging.basicConfig(level=logging.INFO)

logger = logging.getLogger(__name__)


def main() -> None:
    logger.info("Starting")

    loaded_items = DataLoader.load_items("data.json")

    logger.info("Loaded %d items", len(loaded_items))

    # print the first item:
    if loaded_items:
        item_json = loaded_items[0].model_dump_json()
        logger.info("First item: %s", item_json)
    else:
        logger.warning("No items were loaded.")

    model = create_model()

    for item in loaded_items[:10]:
        wiki_summary = get_wikipedia_summary(item.wiki_url)
        logger.info("Wiki summary: %s", wiki_summary)
        output = generate_1(model, item.name, wiki_summary)
        logger.info("%s says: %s", item.name, output)


if __name__ == "__main__":
    main()
