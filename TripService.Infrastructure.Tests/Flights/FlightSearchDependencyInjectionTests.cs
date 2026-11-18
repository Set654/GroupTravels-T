
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TripService.Application.Flights.Interfaces;
using TripService.Infrastructure;
using TripService.Infrastructure.Configuration;
using TripService.Infrastructure.Flights;

namespace TripService.Infrastructure.Tests.Flights;

public class FlightSearchDependencyInjectionTests
{
    [Fact]
    public void AddPersistence_RegistersFlightSearchClient()
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Host=localhost;Database=GroupTravels_Test;Username=test;Password=test",

            ["PythonFlightSearch:BaseUrl"] =
                "http://localhost:8000/",

            ["PythonFlightSearch:ApiKey"] =
                "test-internal-key",

            ["PythonFlightSearch:TimeoutSeconds"] = "30"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();

        services.AddPersistence(configuration);

        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IFlightSearchClient>();

        Assert.NotNull(client);
        Assert.IsType<FlightSearchClient>(client);
    }
}
