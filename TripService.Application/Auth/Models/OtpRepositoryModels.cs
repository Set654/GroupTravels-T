namespace TripService.Application.Auth.Models;

public sealed record OtpReserveRequest(
    string Phone,
    string IpAddress,
    byte[] CodeHmac,
    DateTime RequestedAtUtc);

public enum OtpReserveStatus
{
    Reserved,
    RateLimited,
    PendingAlreadyExists
}

public sealed record OtpReserveResult(
    OtpReserveStatus Status,
    int? ChallengeId = null,
    TimeSpan? RetryAfter = null);

public enum OtpOperationStatus
{
    Success,
    NotFound,
    Conflict
}

public sealed record OtpOperationResult(
    OtpOperationStatus Status);

public enum OtpConsumeStatus
{
    Success,
    InvalidCode,
    Expired,
    AttemptsExceeded,
    NotFound,
    AlreadyUsed,
    Conflict
}

public sealed record OtpConsumeResult(
    OtpConsumeStatus Status,
    int? RemainingAttempts = null);