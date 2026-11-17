
namespace TripService.Domain.Entities;

public class SmsOtpSendAttempt
{
    public long Id { get; set; }

    public required string Phone { get; set; }

    public required string IpAddress { get; set; }

    public DateTime RequestedAtUtc { get; set; }

    public int? ChallengeId { get; set; }

    public SmsOtpChallenge? Challenge { get; set; }
}
