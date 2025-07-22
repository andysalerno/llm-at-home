import json
from typing import Dict

from pydantic import BaseModel

# open the json file:
with open("songs.json", "r") as f:
    data = json.load(f)
    data = data["tracks"]


class Song(BaseModel):
    videoId: str
    title: str
    album: Dict[str, str]
    duration_seconds: int


class SongNext(BaseModel):
    videoId: str
    title: str
    album: str
    duration_seconds: int


next = []

for song in data:
    updated = SongNext(
        videoId=song["videoId"],
        title=song["title"],
        album=song.get("album", {}).get("name", ""),
        duration_seconds=song.get("duration_seconds", 0),
    )

    next.append(updated)


print(f"Loaded {len(data)} songs")

# now write to a clean file:
with open("songs_clean.json", "w") as f:
    json.dump([s.model_dump() for s in next], f, indent=2)
