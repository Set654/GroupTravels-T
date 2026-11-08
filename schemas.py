"""Wire schemas shared by the route, OpenAPI and integration fixtures."""

from datetime import date
from typing import Annotated, Literal

from pydantic import AwareDatetime, BaseModel, ConfigDict, Field
from normalization import MAX_DOTNET_PRICE


class FlightOffer(BaseModel):
    model_config = ConfigDict(extra="forbid")

    origin: str = Field(pattern=r"^[A-Z]{3}$")
    destination: str = Field(pattern=r"^[A-Z]{3}$")
    departureDate: date
    departureAt: AwareDatetime | None
    airline: str | None
    flightNumber: str | None
    price: (Annotated[int, Field(strict=True, gt=0)]
            | Annotated[float, Field(strict=True, gt=0, allow_inf_nan=False)]) = Field(
                description="Positive RUB amount, at most two decimal places; use decimal in C#.",
                json_schema_extra={"format": "decimal", "multipleOf": 0.01, "maximum": MAX_DOTNET_PRICE},
            )
    currency: Literal["RUB"]
    transfers: Annotated[int, Field(strict=True, ge=0)] | None
    durationMinutes: Annotated[int, Field(strict=True, ge=0)] | None
    bookingUrl: str | None


class FlightSearchResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    source: Literal["Aviasales"] = "Aviasales"
    dataType: Literal["cached_offers"] = "cached_offers"
    currency: Literal["RUB"] = "RUB"
    flights: list[FlightOffer]


class ErrorResponse(BaseModel):
    code: str
    message: str


class HealthResponse(BaseModel):
    status: Literal["ok"] = "ok"
