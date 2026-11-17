namespace TripService.Infrastructure.Options;

public sealed class OtpOptions
{
    public const string SectionName = "Otp";

    public int CodeLifetimeMinutes { get; set; } = 5;

    public int MaxAttemptsPerCode { get; set; } = 5;

    public int ResendCooldownSeconds { get; set; } = 60;

    public int MaxRequestsPerPhonePerHour { get; set; } = 3;

    public int MaxRequestsPerPhonePerDay { get; set; } = 10;

    public int MaxRequestsPerIpPerHour { get; set; } = 30;

    public int MaxRequestsPerIpPerDay { get; set; } = 150;

    public int PendingLifetimeSeconds { get; set; } = 120;
}