import logging

from data_loader import DataLoader

logging.basicConfig(level=logging.INFO)

logger = logging.getLogger(__name__)


def main() -> None:
    logger.info("Starting")

    loaded_items = DataLoader.load_items("data.json")

    logger.info("Loaded %d items", len(loaded_items))

    # print the first item:
    if loaded_items:
        item_json = loaded_items[0].model_dump_json(indent=2)
        logger.info("First item: %s", item_json)
    else:
        logger.warning("No items were loaded.")


if __name__ == "__main__":
    main()
