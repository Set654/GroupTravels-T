"""Validation of the flight client's arguments, independent of HTTP endpoints."""

import re
from calendar import monthrange
from datetime import date

from errors import SearchValidationError


def _normalize_code(value: str, name: str) -> str:
    if not isinstance(value, str) or not value.strip():
        raise SearchValidationError(f"{name} must be a nonempty string")

    value = value.strip()
    if re.fullmatch(r"[A-Za-z]{3}", value) is None:
        raise SearchValidationError(f"{name} must contain three Latin letters (city or airport code)")
    return value.upper()


def _date_period(value: str, name: str) -> tuple[str, date, date]:
    if not isinstance(value, str):
        raise SearchValidationError(f"{name} must be a string in YYYY-MM or YYYY-MM-DD format")

    value = value.strip()
    if re.fullmatch(r"[0-9]{4}-[0-9]{2}(?:-[0-9]{2})?", value) is None:
        raise SearchValidationError(f"{name} must use YYYY-MM or YYYY-MM-DD format")

    try:
        first = date.fromisoformat(value + "-01" if len(value) == 7 else value)
    except ValueError:
        raise SearchValidationError(f"{name} must be a valid calendar date or month") from None

    if len(value) == 7:
        last = first.replace(day=monthrange(first.year, first.month)[1])
    else:
        last = first
    return value, first, last


def validate_search_parameters(
    origin: str,
    destination: str,
    departure_at: str,
    return_at: str | None = None,
) -> tuple[str, str, str, str | None]:
    """Normalize codes and dates; reject a return period entirely before departure.

    Months denote whole periods, not their first day. Overlapping periods are
    allowed: for example, a December departure with a December return.
    """
    origin = _normalize_code(origin, "origin")
    destination = _normalize_code(destination, "destination")
    departure_at, departure_start, _ = _date_period(departure_at, "departure_at")

    if return_at is not None:
        return_at, _, return_end = _date_period(return_at, "return_at")
        if return_end < departure_start:
            raise SearchValidationError("return_at cannot be entirely before departure_at")

    return origin, destination, departure_at, return_at
