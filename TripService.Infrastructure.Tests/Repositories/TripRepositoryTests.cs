using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TripService.Domain.Entities;
using TripService.Infrastructure.Persistence;
using TripService.Infrastructure.Repositories;
using Xunit;

namespace TripService.Infrastructure.Tests.Repositories;

public class TripRepositoryTests
{
    [Fact]
    public async Task CreateAsync_CreatesTripAndOwner()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<TripRepositoryTests>()
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

        var repository = new TripRepository(context);

        var user = new User
        {
            Name = "TripRepository Test",
            Phone = "+79990000000",
            Email = $"triptest_{Guid.NewGuid():N}@example.com"
        };

        context.Users.Add(user);

        await context.SaveChangesAsync();

        int tripId = 0;

        try
        {
            tripId = await repository.CreateAsync(
                user.Id,
                "Тестовая поездка",
                new DateOnly(2026, 12, 10),
                new DateOnly(2026, 12, 15),
                null,
                CancellationToken.None);

            var trip = await context.Trips
                .AsNoTracking()
                .SingleAsync(t => t.Id == tripId);

            Assert.Equal("Тестовая поездка", trip.Name);
            Assert.Equal(new DateOnly(2026, 12, 10), trip.StartDate);
            Assert.Equal(new DateOnly(2026, 12, 15), trip.EndDate);

            var owner = await context.TripMembers
                .AsNoTracking()
                .SingleAsync(m =>
                    m.TripId == tripId &&
                    m.UserId == user.Id);

            Assert.Equal("Owner", owner.Role);
            Assert.Equal("Accepted", owner.Status);
            Assert.Null(owner.VirtualName);
        }
        finally
        {
            if (tripId != 0)
            {
                await context.TripMembers
                    .Where(m => m.TripId == tripId)
                    .ExecuteDeleteAsync();

                await context.Trips
                    .Where(t => t.Id == tripId)
                    .ExecuteDeleteAsync();
            }

            await context.Users
                .Where(u => u.Id == user.Id)
                .ExecuteDeleteAsync();
        }
    }
    [Fact]
    public async Task ListAsync_ReturnsOnlyUserTrips()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<TripRepositoryTests>()
            .Build();

        var connectionString = configuration.GetConnectionString("TestConnection")
            ?? throw new InvalidOperationException(
                "Не настроено тестовое подключение.");

        var connectionBuilder =
            new NpgsqlConnectionStringBuilder(connectionString);

        if (connectionBuilder.Database != "GroupTravels_Test")
            throw new InvalidOperationException(
                "Тесты разрешены только для GroupTravels_Test.");

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = new AppDbContext(dbOptions);

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var user1 = new User
        {
            Name = "Первый пользователь",
            Phone = "+79991111111",
            Email = $"user1_{Guid.NewGuid():N}@example.com"
        };

        var user2 = new User
        {
            Name = "Второй пользователь",
            Phone = "+79992222222",
            Email = $"user2_{Guid.NewGuid():N}@example.com"
        };

        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        var repository = new TripRepository(context);

        var today = new DateOnly(2026, 11, 22);

        var trip1Id = await repository.CreateAsync(
            user1.Id,
            "Поездка пользователя 1",
            new DateOnly(2026, 12, 10),
            new DateOnly(2026, 12, 15),
            null,
            CancellationToken.None);

        var trip2Id = await repository.CreateAsync(
            user2.Id,
            "Поездка пользователя 2",
            new DateOnly(2026, 10, 10),
            new DateOnly(2026, 10, 15),
            null,
            CancellationToken.None);

        var trips = await repository.ListAsync(
            user1.Id,
            today,
            CancellationToken.None);

        var trip = Assert.Single(trips);

        Assert.Equal(trip1Id, trip.TripId);
        Assert.Equal("Поездка пользователя 1", trip.TripName);
        Assert.Equal("Upcoming", trip.Status);
        Assert.DoesNotContain(trips, t => t.TripId == trip2Id);

    }
    [Fact]
    public async Task GetAsync_ReturnsTripOnlyForAcceptedMember()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<TripRepositoryTests>()
            .Build();

        var connectionString = configuration
            .GetConnectionString("TestConnection")
            ?? throw new InvalidOperationException(
                "Не настроено подключение TestConnection.");

        var connectionBuilder =
            new NpgsqlConnectionStringBuilder(connectionString);

        if (connectionBuilder.Database != "GroupTravels_Test")
            throw new InvalidOperationException(
                "Тесты разрешены только для GroupTravels_Test.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = new AppDbContext(options);

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var owner = new User
        {
            Name = "Владелец поездки",
            Phone = "+79993333333",
            Email = $"owner_{Guid.NewGuid():N}@example.com"
        };

        var outsider = new User
        {
            Name = "Посторонний пользователь",
            Phone = "+79994444444",
            Email = $"outsider_{Guid.NewGuid():N}@example.com"
        };

        context.Users.AddRange(owner, outsider);
        await context.SaveChangesAsync();

        var repository = new TripRepository(context);

        var today = new DateOnly(2026, 11, 22);

        var tripId = await repository.CreateAsync(
            owner.Id,
            "Поездка в Казань",
            new DateOnly(2026, 11, 20),
            new DateOnly(2026, 11, 25),
            null,
            CancellationToken.None);

        var ownTrip = await repository.GetAsync(
            owner.Id, tripId, today, CancellationToken.None);

        Assert.NotNull(ownTrip);
        Assert.Equal(tripId, ownTrip.TripId);
        Assert.Equal("Поездка в Казань", ownTrip.TripName);
        Assert.Equal("Active", ownTrip.Status);

        var foreignTrip = await repository.GetAsync(
            outsider.Id, tripId, today, CancellationToken.None);

        Assert.Null(foreignTrip);

        var missingTrip = await repository.GetAsync(
            owner.Id, -1, today, CancellationToken.None);

        Assert.Null(missingTrip);
    }
    [Fact]
    public async Task GetAccessAsync_ReturnsAccessOnlyForAcceptedMember()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<TripRepositoryTests>()
            .Build();

        var connectionString = configuration
            .GetConnectionString("TestConnection")
            ?? throw new InvalidOperationException(
                "Не настроено тестовое подключение TestConnection.");

        var connectionBuilder =
            new NpgsqlConnectionStringBuilder(connectionString);

        if (connectionBuilder.Database != "GroupTravels_Test")
            throw new InvalidOperationException(
                "Тесты разрешены только для GroupTravels_Test.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = new AppDbContext(options);

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var owner = new User
        {
            Name = "Владелец",
            Phone = "+79995555555",
            Email = $"access_owner_{Guid.NewGuid():N}@example.com"
        };

        var outsider = new User
        {
            Name = "Посторонний",
            Phone = "+79996666666",
            Email = $"access_outsider_{Guid.NewGuid():N}@example.com"
        };

        context.Users.AddRange(owner, outsider);
        await context.SaveChangesAsync();

        var repository = new TripRepository(context);

        var tripId = await repository.CreateAsync(
            owner.Id,
            "Тест проверки доступа",
            new DateOnly(2026, 12, 10),
            new DateOnly(2026, 12, 15),
            null,
            CancellationToken.None);

        var ownerMember = await context.TripMembers
            .AsNoTracking()
            .SingleAsync(m =>
                m.TripId == tripId &&
                m.UserId == owner.Id);
        var access = await repository.GetAccessAsync(
            owner.Id,
            tripId,
            CancellationToken.None);

        Assert.NotNull(access);
        Assert.Equal(tripId, access.TripId);
        Assert.Equal(ownerMember.Id, access.MemberId);
        Assert.Equal("Owner", access.Role);

        var outsiderAccess = await repository.GetAccessAsync(
            outsider.Id,
            tripId,
            CancellationToken.None);

        Assert.Null(outsiderAccess);

        var pendingMember = new TripMember
        {
            TripId = tripId,
            UserId = outsider.Id,
            VirtualName = null,
            Role = "Member",
            Status = "Pending"
        };

        context.TripMembers.Add(pendingMember);
        await context.SaveChangesAsync();

        var pendingAccess = await repository.GetAccessAsync(
            outsider.Id,
            tripId,
            CancellationToken.None);

        Assert.Null(pendingAccess);
    }
    [Fact]
    public async Task GetMembersAsync_ReturnsRegisteredAndVirtualMembers()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<TripRepositoryTests>()
            .Build();

        var connectionString = configuration
            .GetConnectionString("TestConnection")
            ?? throw new InvalidOperationException(
                "Не настроено тестовое подключение TestConnection.");

        var connectionBuilder =
            new NpgsqlConnectionStringBuilder(connectionString);

        if (connectionBuilder.Database != "GroupTravels_Test")
            throw new InvalidOperationException(
                "Тесты разрешены только для GroupTravels_Test.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = new AppDbContext(options);

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var owner = new User
        {
            Name = "Иван",
            Phone = "+79997777777",
            Email = $"members_{Guid.NewGuid():N}@example.com",
            TbankPaymentUrl = "https://example.com/payment"
        };

        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var repository = new TripRepository(context);

        var tripId = await repository.CreateAsync(
            owner.Id,
            "Поездка в Москву",
            new DateOnly(2026, 12, 10),
            new DateOnly(2026, 12, 15),
            null,
            CancellationToken.None);

        var virtualMember = new TripMember
        {
            TripId = tripId,
            UserId = null,
            VirtualName = "Алексей",
            Role = "Member",
            Status = "Accepted"
        };

        context.TripMembers.Add(virtualMember);
        await context.SaveChangesAsync();

        var members = await repository.GetMembersAsync(
            tripId,
            CancellationToken.None);

        Assert.Equal(2, members.Count);

        var registered = Assert.Single(
            members, m => m.UserId == owner.Id);

        Assert.Equal("Иван", registered.DisplayName);
        Assert.False(registered.IsVirtual);
        Assert.Equal("Owner", registered.Role);
        Assert.Equal("Accepted", registered.Status);
        Assert.Equal(
            "https://example.com/payment",
            registered.TbankPaymentUrl);
        var virtualResult = Assert.Single(
            members, m => m.MemberId == virtualMember.Id);

        Assert.Null(virtualResult.UserId);
        Assert.Equal("Алексей", virtualResult.DisplayName);
        Assert.True(virtualResult.IsVirtual);
        Assert.Equal("Member", virtualResult.Role);
        Assert.Equal("Accepted", virtualResult.Status);
        Assert.Null(virtualResult.TbankPaymentUrl);
    }
    [Fact]
    public async Task AddVirtualAsync_CreatesVirtualMember()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<TripRepositoryTests>()
            .Build();

        var connectionString = configuration
            .GetConnectionString("TestConnection")
            ?? throw new InvalidOperationException(
                "Не настроено тестовое подключение TestConnection.");

        var connectionBuilder =
            new NpgsqlConnectionStringBuilder(connectionString);

        if (connectionBuilder.Database != "GroupTravels_Test")
            throw new InvalidOperationException(
                "Тесты разрешены только для GroupTravels_Test.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = new AppDbContext(options);

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var owner = new User
        {
            Name = "Владелец поездки",
            Phone = $"+7999{Random.Shared.Next(1000000, 9999999)}",
            Email = $"virtual_test_{Guid.NewGuid():N}@example.com"
        };

        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var repository = new TripRepository(context);

        var tripId = await repository.CreateAsync(
            owner.Id,
            "Поездка с виртуальным участником",
            new DateOnly(2026, 12, 10),
            new DateOnly(2026, 12, 15),
            null,
            CancellationToken.None);

        var result = await repository.AddVirtualAsync(
            tripId,
            "Алексей",
            CancellationToken.None);
        Assert.True(result.MemberId > 0);
        Assert.Null(result.UserId);
        Assert.Equal("Алексей", result.DisplayName);
        Assert.True(result.IsVirtual);
        Assert.Equal("Member", result.Role);
        Assert.Equal("Accepted", result.Status);
        Assert.Null(result.TbankPaymentUrl);
        var savedMember = await context.TripMembers
            .AsNoTracking()
            .SingleAsync(m => m.Id == result.MemberId);

        Assert.Equal(tripId, savedMember.TripId);
        Assert.Null(savedMember.UserId);
        Assert.Equal("Алексей", savedMember.VirtualName);
        Assert.Equal("Member", savedMember.Role);
        Assert.Equal("Accepted", savedMember.Status);
    }
}