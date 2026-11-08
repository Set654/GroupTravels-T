"""Server configuration; secret values are excluded from repr()."""

import math
import os
from dataclasses import dataclass, field
from zoneinfo import ZoneInfo, ZoneInfoNotFoundError

from dotenv import load_dotenv

from errors import ConfigurationError


@dataclass(frozen=True)
class Settings:
    travelpayouts_token: str = field(default="", repr=False)
    internal_api_key: str = field(default="", repr=False)
    connect_timeout: float = 3.0
    read_timeout: float = 10.0
    max_concurrent_searches: int = 4
    search_timezone: str = "Europe/Moscow"

    @classmethod
    def from_env(cls) -> "Settings":
        load_dotenv()
        try:
            return cls(
                travelpayouts_token=os.getenv("TRAVELPAYOUTS_TOKEN", "").strip(),
                internal_api_key=os.getenv("INTERNAL_API_KEY", "").strip(),
                connect_timeout=float(os.getenv("PROVIDER_CONNECT_TIMEOUT_SECONDS", "3")),
                read_timeout=float(os.getenv("PROVIDER_READ_TIMEOUT_SECONDS", "10")),
                max_concurrent_searches=int(os.getenv("MAX_CONCURRENT_SEARCHES", "4")),
                search_timezone=os.getenv("SEARCH_TIMEZONE", "Europe/Moscow").strip(),
            )
        except ValueError:
            raise ConfigurationError("Invalid numeric service setting") from None

    def validate(self) -> None:
        if not self.travelpayouts_token:
            raise ConfigurationError("TRAVELPAYOUTS_TOKEN is not configured")
        if (len(self.internal_api_key) < 32
                or any(not 33 <= ord(char) <= 126 for char in self.internal_api_key)):
            raise ConfigurationError("INTERNAL_API_KEY must contain at least 32 visible ASCII characters")
        if self.internal_api_key == self.travelpayouts_token:
            raise ConfigurationError("Internal and provider keys must be different")
        for timeout in (self.connect_timeout, self.read_timeout):
            if not math.isfinite(timeout) or not 0 < timeout <= 60:
                raise ConfigurationError("Provider timeouts must be between 0 and 60 seconds")
        if not 1 <= self.max_concurrent_searches <= 32:
            raise ConfigurationError("MAX_CONCURRENT_SEARCHES must be between 1 and 32")
        try:
            ZoneInfo(self.search_timezone)
        except (ZoneInfoNotFoundError, ValueError):
            raise ConfigurationError("SEARCH_TIMEZONE is not a valid timezone") from None
