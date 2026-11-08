"""Normalize the basic fields of one cached Travelpayouts flight offer."""

import math
import re
from datetime import date, datetime, timezone
from decimal import Decimal

from errors import ProviderResponseError
from booking_links import build_booking_url
from catalog import matches_location

MAX_DOTNET_PRICE = 79228162514264337593543950335


def _required_code(ticket: dict, name: str) -> str:
    value = ticket.get(name)
    if not isinstance(value, str) or re.fullmatch(r"[A-Za-z]{3}", value.strip()) is None:
        raise ProviderResponseError(f"{name} must contain three Latin letters")
    return value.strip().upper()


def _optional_text(value: object, name: str) -> str | None:
    if value is None:
        return None
    if not isinstance(value, str):
        raise ProviderResponseError(f"{name} must be a string or null")
    return value.strip() or None


def normalize_flight(ticket: dict) -> dict:
    """Return five basic fields without modifying the provider's dictionary.

    Prices remain JSON numbers in the currency requested by the caller (RUB
    in the current client). Missing optional text becomes None. Invalid data
    raises ProviderResponseError instead of producing a misleading offer.
    This is a building block, not the complete flights[] response from the SRS.
    """
    if not isinstance(ticket, dict):
        raise ProviderResponseError("Each ticket must be a dictionary")

    origin = _required_code(ticket, "origin")
    destination = _required_code(ticket, "destination")

    price = ticket.get("price")
    if (
        isinstance(price, bool)
        or not isinstance(price, (int, float))
        or (isinstance(price, float) and not math.isfinite(price))
        or price <= 0
    ):
        raise ProviderResponseError("price must be a finite positive number")

    airline = _optional_text(ticket.get("airline"), "airline")
    flight_number = ticket.get("flight_number")
    if type(flight_number) is int:
        if flight_number <= 0:
            raise ProviderResponseError("flight_number must be positive")
        flight_number = str(flight_number)
    flight_number = _optional_text(flight_number, "flight_number")

    return {
        "origin": origin,
        "destination": destination,
        "price": price,
        "airline": airline.upper() if airline is not None else None,
        "flightNumber": flight_number,
    }


def _optional_count(value: object, name: str) -> int | None:
    if value is None:
        return None
    if type(value) is not int or value < 0:
        raise ProviderResponseError(f"{name} must be a nonnegative integer or null")
    return value


def _departure(value: object, requested_date: date) -> datetime | None:
    if value is None or value == "":
        return None
    if not isinstance(value, str):
        raise ProviderResponseError("departure_at must be an ISO date/time or null")
    try:
        if re.fullmatch(r"[0-9]{4}-[0-9]{2}-[0-9]{2}", value):
            actual_date, timestamp = date.fromisoformat(value), None
        else:
            if not re.match(r"[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}", value):
                raise ValueError
            timestamp = datetime.fromisoformat(value)
            actual_date = timestamp.date()
        if actual_date != requested_date:
            raise ValueError
    except ValueError:
        raise ProviderResponseError("departure_at does not match the requested date") from None
    # Never invent an offset for a naive provider timestamp.
    if timestamp is None or timestamp.utcoffset() is None:
        return None
    return timestamp.astimezone(timezone.utc)


def normalize_search_flight(
    ticket: dict, *, origin: str, destination: str, departure_date: date
) -> dict:
    """Normalize a one-way offer into the SRS 3.5 response, without a cache ID."""
    basic = normalize_flight(ticket)
    if (basic["price"] > MAX_DOTNET_PRICE
            or Decimal(str(basic["price"])).as_tuple().exponent < -2):
        raise ProviderResponseError("price must fit .NET decimal with at most two decimal places")
    if not matches_location(origin, basic["origin"], ticket.get("origin_airport")):
        raise ProviderResponseError("Offer origin does not match the request")
    if not matches_location(destination, basic["destination"], ticket.get("destination_airport")):
        raise ProviderResponseError("Offer destination does not match the request")
    if ticket.get("return_at") not in (None, "") or ticket.get("duration_back") not in (None, 0):
        raise ProviderResponseError("Unexpected round-trip offer in a one-way search")
    currency = ticket.get("currency", "RUB")
    if not isinstance(currency, str) or currency.upper() != "RUB":
        raise ProviderResponseError("Offer currency is not RUB")
    duration = ticket.get("duration_to")
    if duration is None:
        duration = ticket.get("duration")
    return {
        **basic,
        "origin": origin,
        "destination": destination,
        "departureDate": departure_date,
        "departureAt": _departure(ticket.get("departure_at"), departure_date),
        "currency": "RUB",
        "transfers": _optional_count(ticket.get("transfers"), "transfers"),
        "durationMinutes": _optional_count(duration, "duration_to"),
        "bookingUrl": build_booking_url(ticket.get("link")),
    }
