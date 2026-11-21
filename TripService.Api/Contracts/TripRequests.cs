namespace TripService.Api.Contracts;

public sealed class CreateTripRequest
{
    public string? TripName { get; set; }
    public string? Name { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? CoverUrl { get; set; }
}
public sealed class AddVirtualMemberRequest
{
    public string? VirtualName { get; set; }
}
