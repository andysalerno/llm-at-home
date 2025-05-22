import logging

from data_loader import DataLoader
from generate_text.gen_1_exclamation import generate_1, generate_2, generate_4
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

    # deduplicate loaded_items by wiki_url:
    seen_urls = set()
    next_loaded_items = []
    for item in loaded_items:
        if item.wiki_url not in seen_urls:
            seen_urls.add(item.wiki_url)
            next_loaded_items.append(item)
    loaded_items = next_loaded_items

    for item in loaded_items[0:20]:
        wiki_summary = get_wikipedia_summary(item.wiki_url)
        # logger.info("Wiki summary: %s", wiki_summary)

        # output = generate_1(model, item.name, wiki_summary)
        # logger.info("    [1] %s says: %s", item.name, output)

        # output = generate_2(model, item.name, wiki_summary)
        # logger.info("    [2] %s says: %s", item.name, output)

        output = generate_4(model, item.name, wiki_summary)
        logger.info("    [4] %s says: %s", item.name, output)


if __name__ == "__main__":
    main()
