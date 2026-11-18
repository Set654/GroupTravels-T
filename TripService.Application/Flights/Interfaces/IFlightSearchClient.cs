using System;
using System.Collections.Generic;
using System.Text;
using TripService.Application.Flights.DTOs;

namespace TripService.Application.Flights.Interfaces
{
    public interface IFlightSearchClient
    {
        Task<FlightSearchResponseDto> SearchAsync(
            string origin,
            string destination,
            DateOnly departureDate,
            CancellationToken cancellationToken = default);
    }
}
