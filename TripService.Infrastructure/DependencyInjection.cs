using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TripService.Infrastructure.Persistence;
using TripService.Infrastructure.Options;
using TripService.Application.Auth.Interfaces;
using TripService.Infrastructure.Persistence.Repositories;
using TripService.Application.Flights.Interfaces;
using TripService.Infrastructure.Configuration;
using TripService.Infrastructure.Flights;
using TripService.Application.Trips;
using TripService.Infrastructure.Repositories;

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
        services.AddScoped<ITripRepository, TripRepository>();

        services.Configure<OtpOptions>(
            configuration.GetSection(OtpOptions.SectionName));
        services.AddScoped<IOtpChallengeRepository, OtpChallengeRepository>();

        services.Configure<PythonFlightSearchOptions>(
    configuration.GetSection(PythonFlightSearchOptions.SectionName));

        services.AddHttpClient<IFlightSearchClient, FlightSearchClient>(
            (serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<
                        Microsoft.Extensions.Options.IOptions<PythonFlightSearchOptions>>()
                    .Value;

                if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri) ||
                    (baseUri.Scheme != Uri.UriSchemeHttp &&
                     baseUri.Scheme != Uri.UriSchemeHttps))
                {
                    throw new InvalidOperationException(
                        "Некорректный PythonFlightSearch:BaseUrl.");
                }

                client.BaseAddress = baseUri;
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false
            });

        return services;
    }
}