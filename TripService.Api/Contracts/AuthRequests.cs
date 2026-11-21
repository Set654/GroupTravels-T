namespace TripService.Api.Contracts;

public sealed class SendCodeRequest
{
    public string? Phone { get; set; }
}
public sealed class VerifyCodeRequest
{
    public string? Phone { get; set; }
    public string? Code { get; set; }
}
