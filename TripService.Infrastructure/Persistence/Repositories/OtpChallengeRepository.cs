using Microsoft.Extensions.Options;
using TripService.Application.Auth.Interfaces;
using TripService.Application.Auth.Models;
using TripService.Infrastructure.Options;
using TripService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using TripService.Domain.Entities;
using System.Security.Cryptography;

namespace TripService.Infrastructure.Persistence.Repositories;

public sealed class OtpChallengeRepository : IOtpChallengeRepository
{
    private readonly AppDbContext _context;
    private readonly OtpOptions _options;

    public OtpChallengeRepository(
        AppDbContext context,
        IOptions<OtpOptions> options)
    {
        _context = context;
        _options = options.Value;
    }


    public async Task<OtpReserveResult> TryReserveAsync(
        OtpReserveRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Phone) ||
            string.IsNullOrWhiteSpace(request.IpAddress) ||
            request.CodeHmac is not { Length: 32 } ||
            request.RequestedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Некорректные параметры OTP.");
        }

        var now = request.RequestedAtUtc;

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);


        var lockKeys = new[]
        {
        $"otp:phone:{request.Phone}",
        $"otp:ip:{request.IpAddress}"
    };

        foreach (var key in lockKeys.OrderBy(
            x => x, StringComparer.Ordinal))
        {
            await OtpLockHelper.LockAsync(
                _context, key, cancellationToken);
        }


        var pending = await _context.SmsOtpChallenges
            .FirstOrDefaultAsync(
                x => x.Phone == request.Phone &&
                     x.Status == "Pending",
                cancellationToken);

        if (pending != null)
        {
            var pendingExpiresAt = pending.CreatedAtUtc
                .AddSeconds(_options.PendingLifetimeSeconds);

            if (pendingExpiresAt > now)
            {
                return new OtpReserveResult(
                    OtpReserveStatus.PendingAlreadyExists);
            }


            pending.Status = "Cancelled";
            await _context.SaveChangesAsync(cancellationToken);
        }


        var dayAgo = now.AddHours(-24);
        var hourAgo = now.AddHours(-1);

        var phoneAttempts = await _context.SmsOtpSendAttempts
            .Where(x => x.Phone == request.Phone &&
                        x.RequestedAtUtc > dayAgo &&
                        x.RequestedAtUtc <= now)
            .Select(x => x.RequestedAtUtc)
            .ToListAsync(cancellationToken);

        var ipAttempts = await _context.SmsOtpSendAttempts
            .Where(x => x.IpAddress == request.IpAddress &&
                        x.RequestedAtUtc > dayAgo &&
                        x.RequestedAtUtc <= now)
            .Select(x => x.RequestedAtUtc)
            .ToListAsync(cancellationToken);


        var lastPhoneRequest = phoneAttempts
            .OrderByDescending(x => x)
            .FirstOrDefault();

        if (lastPhoneRequest != default &&
            lastPhoneRequest.AddSeconds(
                _options.ResendCooldownSeconds) > now)
        {
            var retryAfter = lastPhoneRequest
                .AddSeconds(_options.ResendCooldownSeconds) - now;

            return new OtpReserveResult(
                OtpReserveStatus.RateLimited,
                RetryAfter: retryAfter);
        }


        var phoneHourCount = phoneAttempts.Count(x => x > hourAgo);
        var ipHourCount = ipAttempts.Count(x => x > hourAgo);

        if (phoneHourCount >= _options.MaxRequestsPerPhonePerHour ||
            phoneAttempts.Count >= _options.MaxRequestsPerPhonePerDay ||
            ipHourCount >= _options.MaxRequestsPerIpPerHour ||
            ipAttempts.Count >= _options.MaxRequestsPerIpPerDay)
        {
            return new OtpReserveResult(
                OtpReserveStatus.RateLimited);
        }

        var challenge = new SmsOtpChallenge
        {
            Phone = request.Phone,
            CodeHmac = request.CodeHmac.ToArray(),
            CreatedAtUtc = now,
            AttemptCount = 0,
            Status = "Pending"
        };

        _context.SmsOtpChallenges.Add(challenge);

        await _context.SaveChangesAsync(cancellationToken);


        var attempt = new SmsOtpSendAttempt
        {
            Phone = request.Phone,
            IpAddress = request.IpAddress,
            RequestedAtUtc = now,
            ChallengeId = challenge.Id
        };

        _context.SmsOtpSendAttempts.Add(attempt);

        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new OtpReserveResult(
            OtpReserveStatus.Reserved,
            ChallengeId: challenge.Id);
    }



    public async Task<OtpOperationResult> ActivateAsync(
        int challengeId,
        DateTime activatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (activatedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Время активации должно быть в UTC.");
        }


        var challengePhone = await _context.SmsOtpChallenges
            .AsNoTracking()
            .Where(x => x.Id == challengeId)
            .Select(x => x.Phone)
            .FirstOrDefaultAsync(cancellationToken);

        if (challengePhone == null)
        {
            return new OtpOperationResult(
                OtpOperationStatus.NotFound);
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);


        await OtpLockHelper.LockAsync(
            _context,
            $"otp:phone:{challengePhone}",
            cancellationToken);

        var challenge = await _context.SmsOtpChallenges
            .FirstOrDefaultAsync(
                x => x.Id == challengeId,
                cancellationToken);

        if (challenge == null || challenge.Status != "Pending")
        {
            return new OtpOperationResult(
                OtpOperationStatus.Conflict);
        }


        if (challenge.CreatedAtUtc.AddSeconds(
                _options.PendingLifetimeSeconds) <= activatedAtUtc)
        {
            challenge.Status = "Cancelled";
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new OtpOperationResult(
                OtpOperationStatus.Conflict);
        }

        var previousActive = await _context.SmsOtpChallenges
            .Where(x => x.Phone == challengePhone &&
                        x.Status == "Active")
            .ToListAsync(cancellationToken);

        foreach (var oldChallenge in previousActive)
        {
            oldChallenge.Status = "Cancelled";
        }

        challenge.Status = "Active";
        challenge.ActivatedAtUtc = activatedAtUtc;
        challenge.ExpiresAtUtc = activatedAtUtc.AddMinutes(
            _options.CodeLifetimeMinutes);
        challenge.LastSentAtUtc = activatedAtUtc;

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new OtpOperationResult(
            OtpOperationStatus.Success);
    }



    public async Task<OtpOperationResult> CancelAsync(
        int challengeId,
        DateTime cancelledAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (cancelledAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Время отмены должно быть в UTC.");
        }

        var challengePhone = await _context.SmsOtpChallenges
            .AsNoTracking()
            .Where(x => x.Id == challengeId)
            .Select(x => x.Phone)
            .FirstOrDefaultAsync(cancellationToken);

        if (challengePhone == null)
        {
            return new OtpOperationResult(
                OtpOperationStatus.NotFound);
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        await OtpLockHelper.LockAsync(
            _context,
            $"otp:phone:{challengePhone}",
            cancellationToken);

        var challenge = await _context.SmsOtpChallenges
            .FirstOrDefaultAsync(
                x => x.Id == challengeId,
                cancellationToken);

        if (challenge == null || challenge.Status != "Pending")
        {
            return new OtpOperationResult(
                OtpOperationStatus.Conflict);
        }

        challenge.Status = "Cancelled";

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new OtpOperationResult(
            OtpOperationStatus.Success);
    }



    public async Task<OtpConsumeResult> TryConsumeAsync(
        string phone,
        byte[] candidateHmac,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phone) ||
            candidateHmac is not { Length: 32 } ||
            nowUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Некорректные параметры проверки OTP.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        await OtpLockHelper.LockAsync(
            _context,
            $"otp:phone:{phone}",
            cancellationToken);


        var challenge = await _context.SmsOtpChallenges
            .Where(x => x.Phone == phone &&
                        x.Status == "Active")
            .OrderByDescending(x => x.ActivatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (challenge == null)
        {
            return new OtpConsumeResult(
                OtpConsumeStatus.NotFound);
        }

        if (challenge.ExpiresAtUtc == null ||
            challenge.ExpiresAtUtc <= nowUtc)
        {
            return new OtpConsumeResult(
                OtpConsumeStatus.Expired);
        }

        if (challenge.AttemptCount >= _options.MaxAttemptsPerCode)
        {
            return new OtpConsumeResult(
                OtpConsumeStatus.AttemptsExceeded,
                RemainingAttempts: 0);
        }

        bool isValid = CryptographicOperations.FixedTimeEquals(
            challenge.CodeHmac,
            candidateHmac);

        if (!isValid)
        {
            challenge.AttemptCount++;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new OtpConsumeResult(
                challenge.AttemptCount >= _options.MaxAttemptsPerCode
                    ? OtpConsumeStatus.AttemptsExceeded
                    : OtpConsumeStatus.InvalidCode,
                RemainingAttempts: Math.Max(
                    0,
                    _options.MaxAttemptsPerCode - challenge.AttemptCount));
        }
        challenge.Status = "Consumed";
        challenge.ConsumedAtUtc = nowUtc;

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new OtpConsumeResult(
            OtpConsumeStatus.Success);
    }

}