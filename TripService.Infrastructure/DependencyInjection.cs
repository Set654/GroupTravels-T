using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TripService.Infrastructure.Persistence;
using TripService.Infrastructure.Options;
using TripService.Application.Auth.Interfaces;
using TripService.Infrastructure.Persistence.Repositories;

namespace TripService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Не настроена строка подключения DefaultConnection.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.Configure<OtpOptions>(
            configuration.GetSection(OtpOptions.SectionName));
        services.AddScoped<IOtpChallengeRepository, OtpChallengeRepository>();

        return services;
    }
}