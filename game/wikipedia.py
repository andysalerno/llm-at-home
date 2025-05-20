"""
build_roster.py  -  create people.json with 999 famous (dead) people.

Outputs docs/data/people.json
[
  {
    "code"          : "000",
    "slug"          : "leonardo-da-vinci",
    "name"          : "Leonardo da Vinci",
    "birth_year"    : 1452,
    "death_year"    : 1519,
    "wiki_excerpt"  : "...",
    "thumbnail_url" : "https://upload.wikimedia.org/..."
  },
  ...
]
"""

import json
import logging
import random
import re
import time
from pathlib import Path
from typing import Any

import requests
from tqdm import tqdm

logger = logging.getLogger(__name__)

# ------------------------------------------------------------
# Configuration
# ------------------------------------------------------------
ROSTER_SIZE = 999
OUTPUT_FILE = Path("people.json")

SPARQL_QUERY = """
SELECT ?item ?itemLabel ?enwiki ?birth ?death WHERE {
  ?item wdt:P31 wd:Q5.          # instance of human
  ?item wdt:P569 ?birth.
  ?item wdt:P570 ?death.        # has death date → must be dead
  ?item sitelink:enwiki ?enwiki.
  # OPTIONAL { ?item wdt:P166 ?award. } # (Example filter for "famous")
  SERVICE wikibase:label { bd:serviceParam wikibase:language "en". }
}
LIMIT 50000
"""
ENDPOINT = "https://query.wikidata.org/sparql"
HEADERS = {"User-Agent": "TemporalDatingMachine/0.1"}

SUMMARY_API = "https://en.wikipedia.org/api/rest_v1/page/summary/{title}"

# ------------------------------------------------------------
# Helpers
# ------------------------------------------------------------
_slug_re = re.compile(r"[^\w]+")


def slugify(text: str) -> str:
    return _slug_re.sub("-", text.lower()).strip("-")


def sparql_fetch() -> list[dict[str, Any]]:
    logger.info("Querying Wikidata…")
    r = requests.get(
        ENDPOINT,
        params={"query": SPARQL_QUERY, "format": "json"},
        headers=HEADERS,
        timeout=60,
    )
    r.raise_for_status()
    records = []
    for b in r.json()["results"]["bindings"]:
        records.append(
            {
                "qid": b["item"]["value"].split("/")[-1],
                "name": b["itemLabel"]["value"],
                "title": b["enwiki"]["value"].split("wiki/")[-1],
                "birth": int(b["birth"]["value"][:4]),
                "death": int(b["death"]["value"][:4]),
            }
        )

    logger.info("%s Wikidata rows fetched.", len(records))
    return records


def fetch_summary(title: str) -> dict[str, Any]:
    url = SUMMARY_API.format(title=title)
    r = requests.get(url, headers=HEADERS, timeout=20)
    if r.status_code == 404:
        return {}
    r.raise_for_status()
    j = r.json()
    return {
        "extract": j.get("extract", "")[:700],  # trim to 700 chars
        "thumbnail": j.get("thumbnail", {}).get("source"),
    }


def generate(seed: int) -> None:
    records = sparql_fetch()

    rng = random.Random(seed)
    rng.shuffle(records)
    sample = records[:ROSTER_SIZE]

    roster = []
    for idx, rec in enumerate(tqdm(sample, desc="Fetching wiki summaries")):
        summ = {}
        retries = 3
        while retries:
            try:
                summ = fetch_summary(rec["title"])
                break
            except requests.RequestException:
                retries -= 1
                time.sleep(1.5)
        roster.append(
            {
                "code": f"{idx:03d}",
                "slug": slugify(rec["name"]),
                "name": rec["name"],
                "birth_year": rec["birth"],
                "death_year": rec["death"],
                "wiki_excerpt": summ.get("extract", ""),
                "thumbnail_url": summ.get("thumbnail"),
            }
        )

    OUTPUT_FILE.parent.mkdir(parents=True, exist_ok=True)
    with OUTPUT_FILE.open("w", encoding="utf-8") as f:
        json.dump(roster, f, ensure_ascii=False, indent=2)

    logger.info("Wrote %s", OUTPUT_FILE)


if __name__ == "__main__":
    generate(42)
