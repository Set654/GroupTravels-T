
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;
using TripService.Application.Auth.Models;
using TripService.Infrastructure.Options;
using TripService.Infrastructure.Persistence;
using TripService.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TripService.Infrastructure.Tests.Repositories;

public class OtpChallengeRepositoryTests
{
    [Fact]
    public async Task TryReserveAsync_CreatesPendingOtp()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<OtpChallengeRepositoryTests>()
            .Build();

        var connectionString = configuration
            .GetConnectionString("TestConnection")
            ?? throw new InvalidOperationException(
                "Не настроено тестовое подключение TestConnection.");


        var connectionBuilder =
            new NpgsqlConnectionStringBuilder(connectionString);

        if (connectionBuilder.Database != "GroupTravels_Test")
        {
            throw new InvalidOperationException(
                "Интеграционные тесты разрешены только для GroupTravels_Test.");
        }


        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = new AppDbContext(dbOptions);
        var otpOptions = Microsoft.Extensions.Options.Options.Create(
            new OtpOptions());

        var repository = new OtpChallengeRepository(
            context,
            otpOptions);
        var phone = "+7999" +
            RandomNumberGenerator.GetInt32(1000000, 9999999);

        var hmac = RandomNumberGenerator.GetBytes(32);

        var request = new OtpReserveRequest(
            Phone: phone,
            IpAddress: "192.0.2.10",
            CodeHmac: hmac,
            RequestedAtUtc: DateTime.UtcNow);

        var result = await repository.TryReserveAsync(request);


        Assert.Equal(OtpReserveStatus.Reserved, result.Status);
        Assert.NotNull(result.ChallengeId);

        var challenge = await context.SmsOtpChallenges
            .SingleAsync(x => x.Id == result.ChallengeId!.Value);

        Assert.Equal("Pending", challenge.Status);
        Assert.Equal(phone, challenge.Phone);
        Assert.Equal(0, challenge.AttemptCount);

        var attemptExists = await context.SmsOtpSendAttempts
            .AnyAsync(x => x.ChallengeId == challenge.Id);

        Assert.True(attemptExists);
    }
}
