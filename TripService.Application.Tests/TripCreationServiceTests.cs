using TripService.Application.Trips;

namespace TripService.Application.Tests;

public sealed class TripCreationServiceTests
{
    [Fact]
    public void PrepareTrip_ValidData_ReturnsTripWithTrimmedName()
    {
        var service = new CreateTripService(null!);

        var trip = service.PrepareTrip(
            "  Казань  ",
            new DateOnly(2026, 12, 10),
            new DateOnly(2026, 12, 15),
            null);

        Assert.Equal("Казань", trip.Name);
        Assert.Equal(new DateOnly(2026, 12, 10), trip.StartDate);
        Assert.Equal(new DateOnly(2026, 12, 15), trip.EndDate);
        Assert.Equal(0, trip.Id); // No database insert has taken place.
    }

    [Fact]
    public void PrepareTrip_EmptyName_ThrowsArgumentException()
    {
        var service = new CreateTripService(null!);
        Assert.Throws<ArgumentException>(() => service.PrepareTrip(
            "  ", new DateOnly(2026, 12, 10),
            new DateOnly(2026, 12, 15), null));
    }

    [Fact]
    public void PrepareTrip_EndBeforeStart_ThrowsArgumentException()
    {
        var service = new CreateTripService(null!);
        Assert.Throws<ArgumentException>(() => service.PrepareTrip(
            "Казань", new DateOnly(2026, 12, 10),
            new DateOnly(2026, 12, 1), null));
    }

    [Fact]
    public void PrepareTrip_NonHttpsCover_ThrowsArgumentException()
    {
        var service = new CreateTripService(null!);
        Assert.Throws<ArgumentException>(() => service.PrepareTrip(
            "Казань", new DateOnly(2026, 12, 10),
            new DateOnly(2026, 12, 15), "http://example.org/image.jpg"));
    }
}
