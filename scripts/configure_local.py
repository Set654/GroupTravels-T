"""Create the local internal key without printing it or replacing an existing key."""

import os
import secrets
from pathlib import Path

from dotenv import dotenv_values, set_key


def main() -> None:
    root = Path(__file__).resolve().parents[1]
    destination = root / ".env"
    if not destination.exists():
        destination.write_text((root / ".env.example").read_text(encoding="utf-8"), encoding="utf-8")
    values = dotenv_values(destination)
    if not (os.getenv("INTERNAL_API_KEY") or values.get("INTERNAL_API_KEY")):
        set_key(str(destination), "INTERNAL_API_KEY", secrets.token_hex(32))
    print("Internal key configured locally; its value was not printed.")
    print("Provider token configured:", bool(os.getenv("TRAVELPAYOUTS_TOKEN") or values.get("TRAVELPAYOUTS_TOKEN")))


if __name__ == "__main__":
    main()
