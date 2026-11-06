import argparse
import json
import os
from datetime import datetime
from pathlib import Path

import requests
from dotenv import load_dotenv

from errors import ConfigurationError, ProviderResponseError, ProviderSearchError
from validation import validate_search_parameters

load_dotenv()


TOKEN = os.getenv("TRAVELPAYOUTS_TOKEN")

def get_cheap_bilets(
    origin: str,
    destination: str,
    departure_at: str,
    return_at: str | None = None,
) -> list[dict]:
    """Return cached offers, an empty list, or an exception on failure."""
    url = 'https://api.travelpayouts.com/aviasales/v3/prices_for_dates'

    if not isinstance(TOKEN, str) or not TOKEN.strip():
        raise ConfigurationError("TRAVELPAYOUTS_TOKEN is not configured")

    origin, destination, departure_at, return_at = validate_search_parameters(
        origin, destination, departure_at, return_at
    )

    headers = {
        'X-Access-Token': TOKEN.strip()
    }

    params = {
        "destination": destination,
        "origin": origin,
        "departure_at": departure_at,
        "limit": 10,
        "currency": "rub",
        "one_way": "true",
        "return_at": return_at,
        "sorting": "price"
    }
    
    if return_at is not None:
        params["one_way"] = "false"

    filtered_params = {key: value for key, value in params.items() if value is not None}

    
    response = requests.get(url, params=filtered_params, headers=headers, timeout=10)
    response.raise_for_status()
    


    data = response.json()
    if not isinstance(data, dict):
        raise ProviderResponseError("API response is not a dictionary")
    if data.get("success") is not True:
        raise ProviderSearchError("API did not confirm a successful search")
    tickets = data.get("data")
    if not isinstance(tickets, list):
        raise ProviderResponseError("tickets must be a list")
    for ticket in tickets:
        if not isinstance(ticket, dict):
            raise ProviderResponseError("Each ticket must be a dictionary")
    return tickets

def save_tickets(
    origin: str,
    destination: str,
    departure_at: str,
    tickets: list[dict],
    return_at: str | None = None,
    filename: str | Path = "tickets.json",
) -> None:
    """Save a nonempty result for local debugging, replacing the previous file."""

    if not tickets:
        return
    origin, destination, departure_at, return_at = validate_search_parameters(
        origin, destination, departure_at, return_at
    )
    now = datetime.now().isoformat(timespec="seconds")
    data_to_save = {
        "origin": origin,
        "destination": destination,
        "departure_at": departure_at,
        "requested_at": now,
        "return_at": return_at,
        "tickets": tickets
    }
    with open(filename, "w", encoding="utf-8") as file:
        json.dump(data_to_save, file, ensure_ascii=False, indent=4)


def cli(argv: list[str] | None = None) -> int:
    """Run a manual search without changing source files."""
    parser = argparse.ArgumentParser(description="Поиск кэшированных авиапредложений")
    parser.add_argument("--origin", required=True, help="Код города или аэропорта, например MOW")
    parser.add_argument("--destination", required=True, help="Код назначения, например LED")
    parser.add_argument("--departure-at", required=True, help="YYYY-MM или YYYY-MM-DD")
    parser.add_argument("--return-at", help="Дата/месяц возврата; пропустить для одного направления")
    parser.add_argument("--output", type=Path, help="Файл для сохранения непустого результата")
    args = parser.parse_args(argv)

    try:
        tickets = get_cheap_bilets(
            args.origin, args.destination, args.departure_at, return_at=args.return_at
        )
        if args.output is not None:
            save_tickets(
                args.origin,
                args.destination,
                args.departure_at,
                tickets,
                return_at=args.return_at,
                filename=args.output,
            )
    except (requests.exceptions.RequestException, ValueError, RuntimeError, OSError) as error:
        parser.exit(status=1, message=f"Ошибка: {error}\n")

    print(json.dumps(tickets, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(cli())


    
