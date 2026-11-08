"""Offline location reference from Travelpayouts, shared by validation and matching."""

import json
from functools import lru_cache
from pathlib import Path

from errors import ConfigurationError

CATALOG_PATH = Path(__file__).parent / "data" / "iata_locations.json"


@lru_cache(maxsize=1)
def load_locations() -> dict[str, str]:
    try:
        payload = json.loads(CATALOG_PATH.read_text(encoding="utf-8"))
        locations = payload["locations"]
        if not isinstance(locations, dict) or not {"MOW", "LED", "SVO"} <= locations.keys():
            raise ValueError
        if any(not isinstance(k, str) or not isinstance(v, str)
               or len(k) != 3 or len(v) != 3 for k, v in locations.items()):
            raise ValueError
        return locations
    except (OSError, ValueError, KeyError, TypeError):
        raise ConfigurationError("Location reference is missing or invalid") from None


def matches_location(requested: str, actual: str, airport: object) -> bool:
    locations = load_locations()
    # A specific airport must not silently become a different airport in its city.
    if locations.get(requested) != requested:
        if airport is None:
            return actual == requested
        return airport == requested and locations.get(actual) == locations.get(requested)
    return (locations.get(actual) == requested
            and (airport is None or (isinstance(airport, str) and locations.get(airport) == requested)))
