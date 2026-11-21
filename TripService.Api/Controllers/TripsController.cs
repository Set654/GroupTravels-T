using Microsoft.AspNetCore.Mvc;
using TripService.Api.Contracts;
using TripService.Application.Trips;

namespace TripService.Api.Controllers;

[ApiController]
[Route("api/trips")]
public sealed class TripsController : ControllerBase
{
    private readonly CreateTripService _createTripService;
    private readonly ILogger<TripsController> _logger;

    public TripsController(CreateTripService createTripService, ILogger<TripsController> logger)
    {
        _createTripService = createTripService;
        _logger = logger;
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateTripRequest request)
    {
        try
        {

            if (request.TripName is not null && request.Name is not null &&
                !string.Equals(request.TripName, request.Name, StringComparison.Ordinal))
            {
                throw new ArgumentException("The name and tripName fields must match.");
            }

            var trip = _createTripService.PrepareTrip(
                request.TripName ?? request.Name,
                request.StartDate,
                request.EndDate,
                request.CoverUrl);

            _logger.LogInformation("Validated trip request for {TripName}", trip.Name);

            return StatusCode(StatusCodes.Status501NotImplemented);
        }
        catch (ArgumentException)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Проверьте название и даты поездки.",
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "INVALID_TRIP_DATA"
                });
        }
    }

    [HttpGet("{tripId:int}")]
    public IActionResult GetById(int tripId)
    {
        // TODO: После реализации ITripRepository и аутентификации пользователя
        // подключить GetTripService и вернуть HTTP 200 либо HTTP 404.
        return StatusCode(StatusCodes.Status501NotImplemented);
    }
}
