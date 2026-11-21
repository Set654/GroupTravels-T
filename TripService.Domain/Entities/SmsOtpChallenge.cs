
namespace TripService.Domain.Entities;

public class SmsOtpChallenge
{
    public int Id { get; set; }

    public required string Phone { get; set; }

    public required byte[] CodeHmac { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ActivatedAtUtc { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    public int AttemptCount { get; set; }

    public DateTime? LastSentAtUtc { get; set; }

    public DateTime? ConsumedAtUtc { get; set; }

    public required string Status { get; set; }
}
