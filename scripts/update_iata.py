"""Refresh the committed location reference from fixed official HTTPS endpoints."""

import json
import re
from datetime import datetime, timezone
from pathlib import Path

import requests


def main() -> None:
    sources = {
        "cities": "https://api.travelpayouts.com/data/en/cities.json",
        "airports": "https://api.travelpayouts.com/data/en/airports.json",
    }
    datasets = {}
    for name, url in sources.items():
        response = requests.get(url, timeout=(5, 30), allow_redirects=False)
        response.raise_for_status()
        if response.status_code != 200:
            raise RuntimeError("Unexpected reference response")
        datasets[name] = response.json()
        if not isinstance(datasets[name], list):
            raise RuntimeError("Invalid location reference")

    locations = {}
    for city in datasets["cities"]:
        code = city.get("code")
        if isinstance(code, str) and re.fullmatch(r"[A-Z]{3}", code):
            locations[code] = code
    for airport in datasets["airports"]:
        code, city = airport.get("code"), airport.get("city_code")
        if isinstance(code, str) and re.fullmatch(r"[A-Z]{3}", code):
            locations[code] = city if city in locations else code
    if len(locations) < 1000 or locations.get("SVO") != "MOW":
        raise RuntimeError("Incomplete location reference")
    destination = Path(__file__).resolve().parents[1] / "data" / "iata_locations.json"
    destination.parent.mkdir(exist_ok=True)
    payload = {
        "sources": list(sources.values()),
        "retrievedAt": datetime.now(timezone.utc).isoformat(),
        "locations": dict(sorted(locations.items())),
    }
    destination.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    print(f"Saved {len(locations)} location codes")


if __name__ == "__main__":
    main()
