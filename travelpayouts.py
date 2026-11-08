"""The external HTTP client. This module has no FastAPI or persistence code."""

import requests
from urllib3.exceptions import ReadTimeoutError

from errors import ConfigurationError, ProviderResponseError, ProviderSearchError
from validation import validate_search_parameters

SEARCH_URL = "https://api.travelpayouts.com/aviasales/v3/prices_for_dates"


def fetch_offers(
    origin: str,
    destination: str,
    departure_at: str,
    return_at: str | None = None,
    *,
    token: str,
    timeout: float | tuple[float, float] = 10,
) -> list[dict]:
    if not isinstance(token, str) or not token.strip():
        raise ConfigurationError("TRAVELPAYOUTS_TOKEN is not configured")
    origin, destination, departure_at, return_at = validate_search_parameters(
        origin, destination, departure_at, return_at
    )
    params = {
        "origin": origin, "destination": destination, "departure_at": departure_at,
        "limit": 10, "currency": "rub", "one_way": "true", "sorting": "price",
    }
    if return_at is not None:
        params.update(return_at=return_at, one_way="false")
    try:
        response = requests.get(
            SEARCH_URL, params=params, headers={"X-Access-Token": token.strip()},
            timeout=timeout, allow_redirects=False,
        )
    except requests.exceptions.ConnectionError as exc:
        # Requests can wrap a body-read timeout in ConnectionError when stream=False.
        if any(isinstance(cause, ReadTimeoutError) for cause in exc.args):
            raise requests.exceptions.ReadTimeout("Provider response body timed out") from None
        raise
    try:
        response.raise_for_status()
        if 300 <= response.status_code < 400:
            raise ProviderResponseError("Unexpected redirect from provider")
        data = response.json()
    finally:
        response.close()
    if not isinstance(data, dict):
        raise ProviderResponseError("API response is not a dictionary")
    if data.get("success") is not True:
        raise ProviderSearchError("API did not confirm a successful search")
    currency = data.get("currency", "RUB")
    if not isinstance(currency, str) or currency.upper() != "RUB":
        raise ProviderResponseError("Provider returned an unexpected currency")
    tickets = data.get("data")
    if not isinstance(tickets, list):
        raise ProviderResponseError("tickets must be a list")
    if len(tickets) > 10:
        raise ProviderResponseError("Provider exceeded the requested result limit")
    for ticket in tickets:
        if not isinstance(ticket, dict):
            raise ProviderResponseError("Each ticket must be a dictionary")
    return tickets
