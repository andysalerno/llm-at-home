from ytmusicapi import YTMusic
import csv
import json

yt = YTMusic()  # works without cookies for public data
artist = yt.search("The Beatles", filter="artists")[0]
beatles_id = artist["browseId"]

songs = yt.get_artist(beatles_id)["songs"]
browse_id = songs["browseId"]
print(f"songs: {json.dumps(songs, indent=2)}")
songs = songs["results"]

playlist = yt.get_playlist(browse_id)
print(f"playlist:\n{json.dumps(playlist)}")


# print(f"Found {len(songs)} songs for The Beatles")
# print(f"{json.dumps(songs, indent=2)}")

rows = []
for s in songs:
    rows.append(
        {
            "title": s["title"],
            "album": s.get("album", {}).get("name", ""),
            "duration": s.get("duration"),
            "video_id": s["videoId"],
            "url": f"https://www.youtube.com/watch?v={s['videoId']}",
        }
    )

with open("beatles_youtube_urls.csv", "w", newline="") as f:
    writer = csv.DictWriter(f, rows[0].keys())
    writer.writeheader()
    writer.writerows(rows)
print(f"Wrote {len(rows)} rows")
