using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripService.Api.Contracts;
using TripService.Api.Extensions;
using TripService.Application.Trips;

namespace TripService.Api.Controllers;

[ApiController]
[Route("api/trips")]
[Authorize]
public sealed class TripsController : ControllerBase
{
    private readonly CreateTripService _createTripService;
    private readonly TripQueryService _tripQueryService;

    public TripsController(
        CreateTripService createTripService,
        TripQueryService tripQueryService)
    {
        _createTripService = createTripService;
        _tripQueryService = tripQueryService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTripRequest request,
        CancellationToken ct)
    {
        if (!User.TryGetUserId(out int userId))
        {
            return Unauthorized();
        }

        try
        {
            if (request.TripName is not null &&
                request.Name is not null &&
                request.TripName != request.Name)
            {
                throw new ArgumentException(
                    "Поля name и tripName должны совпадать.");
            }

            var tripId = await _createTripService.CreateAsync(
                userId,
                request.TripName ?? request.Name,
                request.StartDate,
                request.EndDate,
                request.CoverUrl,
                ct);

            return CreatedAtAction(
                nameof(GetById),
                new { tripId },
                new { tripId });
        }
        catch (ArgumentException)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Проверьте название, даты и обложку поездки.",
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "INVALID_TRIP_DATA"
                });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken ct)
    {
        if (!User.TryGetUserId(out int userId))
        {
            return Unauthorized();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var trips = await _tripQueryService.ListAsync(
            userId,
            today,
            ct);

        return Ok(trips);
    }

    [HttpGet("{tripId:int}")]
    public async Task<IActionResult> GetById(
        int tripId,
        CancellationToken ct)
    {
        if (!User.TryGetUserId(out int userId))
        {
            return Unauthorized();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var trip = await _tripQueryService.GetAsync(
            userId,
            tripId,
            today,
            ct);

        if (trip is null)
        {
            return NotFound();
        }

        return Ok(trip);
    }
}