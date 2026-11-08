"""Flight search use case, independent of HTTP and database ownership."""

from collections.abc import Callable
from datetime import date, datetime
from threading import BoundedSemaphore
from zoneinfo import ZoneInfo

from errors import ServiceBusyError
from normalization import normalize_search_flight
from schemas import FlightSearchResponse
from settings import Settings
from travelpayouts import fetch_offers
from validation import validate_mvp_search


class FlightService:
    def __init__(self, settings: Settings, *, today: Callable[[], date] | None = None):
        settings.validate()
        self.settings = settings
        self.today = today or (lambda: datetime.now(ZoneInfo(settings.search_timezone)).date())
        self.slots = BoundedSemaphore(settings.max_concurrent_searches)

    def search(self, origin: str, destination: str, departure_date: str) -> FlightSearchResponse:
        origin, destination, departure = validate_mvp_search(
            origin, destination, departure_date, today=self.today()
        )
        if not self.slots.acquire(blocking=False):
            raise ServiceBusyError("Concurrent search limit reached")
        try:
            tickets = fetch_offers(
                origin, destination, departure.isoformat(),
                token=self.settings.travelpayouts_token,
                timeout=(self.settings.connect_timeout, self.settings.read_timeout),
            )
            # An invalid provider row fails the search rather than hiding corruption as [].
            flights = [normalize_search_flight(
                ticket, origin=origin, destination=destination, departure_date=departure
            ) for ticket in tickets]
            flights.sort(key=lambda flight: flight["price"])
            return FlightSearchResponse(flights=flights)
        finally:
            self.slots.release()
