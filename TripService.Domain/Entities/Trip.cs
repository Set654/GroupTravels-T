namespace TripService.Domain.Entities;

public class Trip
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public string? CoverUrl { get; set; }
}
