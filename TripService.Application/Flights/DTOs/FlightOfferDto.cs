using System;
using System.Collections.Generic;
using System.Text;

namespace TripService.Application.Flights.DTOs
{
    public sealed class FlightOfferDto
    {

        public required string Origin { get; init; }
        public required string Destination { get; init; }
        public DateOnly DepartureDate { get; init; }
        public DateTimeOffset? DepartureAt { get; init; }
        public string? Airline { get; init; }
        public string? FlightNumber { get; init; }
        public decimal Price { get; init; }

        public string Currency { get; init; } = "RUB";

        public int? Transfers { get; init; }

        public int? DurationMinutes { get; init; }

        public string? BookingUrl { get; init; }
    }
}
