"""Export OpenAPI and synthetic integration examples without tokens or HTTP calls."""

import json
import sys
from datetime import date
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))

from api import create_app
from normalization import normalize_search_flight
from schemas import FlightSearchResponse
from settings import Settings


def write_json(path: Path, content: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(content, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main() -> None:
    # These are public placeholders for schema generation, not operational secrets.
    settings = Settings(travelpayouts_token="schema-placeholder",
                        internal_api_key="schema-placeholder-not-a-real-key-0000")
    write_json(ROOT / "docs" / "openapi.json", create_app(settings).openapi())
    tickets = json.loads((ROOT / "examples" / "provider_offers.json").read_text(encoding="utf-8"))
    flights = [normalize_search_flight(ticket, origin="MOW", destination="LED",
                                     departure_date=date(2026, 12, 15)) for ticket in tickets]
    flights.sort(key=lambda flight: flight["price"])
    write_json(ROOT / "examples" / "responses" / "search-success.json",
               FlightSearchResponse(flights=flights).model_dump(mode="json"))
    write_json(ROOT / "examples" / "responses" / "search-empty.json",
               FlightSearchResponse(flights=[]).model_dump(mode="json"))
    for status, code, message in (
        (400, "INVALID_SEARCH_PARAMETERS", "Параметры поиска некорректны."),
        (401, "UNAUTHORIZED", "Неверный или отсутствующий ключ внутреннего сервиса."),
        (500, "INTERNAL_ERROR", "Внутренняя ошибка сервиса."),
        (502, "FLIGHT_PROVIDER_ERROR", "Не удалось получить корректные предложения провайдера."),
        (503, "FLIGHT_SERVICE_UNAVAILABLE", "Соединение с провайдером недоступно."),
        (504, "FLIGHT_SEARCH_TIMEOUT", "Провайдер не ответил вовремя."),
    ):
        write_json(ROOT / "examples" / "responses" / f"error-{status}.json",
                   {"code": code, "message": message})
    print("Exported OpenAPI and synthetic response examples")


if __name__ == "__main__":
    main()
