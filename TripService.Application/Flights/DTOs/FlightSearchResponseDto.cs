using System;
using System.Collections.Generic;
using System.Text;

namespace TripService.Application.Flights.DTOs
{
    public sealed class FlightSearchResponseDto
    {
        public string Source { get; init; } = "Aviasales";
        public string DataType { get; init; } = "cached_offers";
        public string Currency { get; init; } = "RUB";
        public List<FlightOfferDto> Flights { get; init; } = new();
    }
}
