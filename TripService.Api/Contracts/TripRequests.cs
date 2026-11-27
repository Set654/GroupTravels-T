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
public sealed class InviteMemberRequest
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public sealed class RespondInvitationRequest
{
    public int MemberId { get; set; }
    public string? Decision { get; set; }
}
