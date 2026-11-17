using TripService.Application.Auth.Models;

namespace TripService.Application.Auth.Interfaces;

public interface IOtpChallengeRepository
{
    Task<OtpReserveResult> TryReserveAsync(
        OtpReserveRequest request,
        CancellationToken cancellationToken = default);

    Task<OtpOperationResult> ActivateAsync(
        int challengeId,
        DateTime activatedAtUtc,
        CancellationToken cancellationToken = default);

    Task<OtpOperationResult> CancelAsync(
        int challengeId,
        DateTime cancelledAtUtc,
        CancellationToken cancellationToken = default);

    Task<OtpConsumeResult> TryConsumeAsync(
        string phone,
        byte[] candidateHmac,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);
}