"""Build browser links without making requests to provider-supplied URLs."""

import re
from urllib.parse import unquote, urlsplit, urlunsplit

from errors import ProviderResponseError

ALLOWED_HOSTS = frozenset({"aviasales.ru", "www.aviasales.ru", "aviasales.com", "www.aviasales.com"})


def build_booking_url(value: object) -> str | None:
    if value is None or value == "":
        return None
    if not isinstance(value, str) or len(value) > 16384:
        raise ProviderResponseError("link must be an Aviasales search URL")
    # Browsers and URL parsers disagree on backslashes and control characters.
    if re.search(r"[\s\\\x00-\x1f\x7f]", value):
        raise ProviderResponseError("Unsafe characters in link")
    try:
        parts = urlsplit(value)
        if value.startswith("/search/"):
            parts = urlsplit("https://www.aviasales.ru" + value)
        if (parts.scheme != "https" or parts.hostname not in ALLOWED_HOSTS
                or parts.username is not None or parts.password is not None
                or parts.port not in (None, 443) or parts.fragment):
            raise ValueError
        decoded_path = parts.path
        for _ in range(3):
            decoded_path = unquote(decoded_path)
        if (not decoded_path.startswith("/search/") or decoded_path == "/search/"
                or any(segment in (".", "..") for segment in decoded_path.split("/"))
                or re.search(r"[\s\\\x00-\x1f\x7f]", decoded_path)):
            raise ValueError
    except ValueError:
        raise ProviderResponseError("link must be an allowed HTTPS Aviasales search URL") from None
    return urlunsplit(("https", parts.hostname, parts.path, parts.query, ""))
