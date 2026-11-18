
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using TripService.Infrastructure.Configuration;
using TripService.Infrastructure.Flights;
using Xunit;

namespace TripService.Infrastructure.Tests.Flights;

public class FlightSearchClientTests
{
    [Fact]
    public async Task SearchAsync_SendsCorrectRequest()
    {
        using var handler = new FakeHttpHandler();

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8000/"),
            Timeout = TimeSpan.FromSeconds(30)
        };

        var options = Microsoft.Extensions.Options.Options.Create(
            new PythonFlightSearchOptions
            {
                ApiKey = "test-internal-key"
            });

        var client = new FlightSearchClient(httpClient, options);

        var result = await client.SearchAsync(
            "MOW",
            "LED",
            new DateOnly(2026, 12, 10));

        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.Equal(
            "/internal/flights/search?origin=MOW&destination=LED&departureDate=2026-12-10",
            handler.RequestUri);

        Assert.Equal("test-internal-key", handler.ApiKey);
        Assert.Empty(result.Flights);
        Assert.Equal("RUB", result.Currency);

    }

    [Fact]
    public async Task SearchAsync_WhenPythonReturns500_Throws502()
    {
        using var handler = new ErrorHttpHandler();

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8000/")
        };

        var options = Microsoft.Extensions.Options.Options.Create(
            new PythonFlightSearchOptions
            {
                ApiKey = "test-internal-key"
            });

        var client = new FlightSearchClient(httpClient, options);

        var exception = await Assert.ThrowsAsync<FlightSearchException>(
            () => client.SearchAsync(
                "MOW",
                "LED",
                new DateOnly(2026, 12, 10)));

        Assert.Equal(502, exception.StatusCode);
        Assert.Equal("FLIGHT_PROVIDER_ERROR", exception.ErrorCode);
    }
    [Fact]
    public async Task SearchAsync_WhenPythonReturns429_ThrowsRateLimited()
    {
        using var handler = new RateLimitHttpHandler();

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8000/")
        };

        var options = Microsoft.Extensions.Options.Options.Create(
            new PythonFlightSearchOptions
            {
                ApiKey = "test-internal-key"
            });

        var client = new FlightSearchClient(httpClient, options);

        var exception = await Assert.ThrowsAsync<FlightSearchException>(
            () => client.SearchAsync(
                "MOW",
                "LED",
                new DateOnly(2026, 12, 10)));

        Assert.Equal(429, exception.StatusCode);
        Assert.Equal(
            "FLIGHT_SEARCH_RATE_LIMITED",
            exception.ErrorCode);
    }

    [Fact]
    public async Task SearchAsync_WhenPythonReturnsInvalidJson_Throws502()
    {
        using var handler = new InvalidJsonHttpHandler();

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8000/")
        };

        var options = Microsoft.Extensions.Options.Options.Create(
            new PythonFlightSearchOptions
            {
                ApiKey = "test-internal-key"
            });

        var client = new FlightSearchClient(httpClient, options);

        var exception = await Assert.ThrowsAsync<FlightSearchException>(
            () => client.SearchAsync(
                "MOW",
                "LED",
                new DateOnly(2026, 12, 10)));

        Assert.Equal(502, exception.StatusCode);
        Assert.Equal(
            "FLIGHT_PROVIDER_INVALID_RESPONSE",
            exception.ErrorCode);
    }

    [Fact]
    public async Task SearchAsync_WhenPythonTimesOut_Throws504()
    {
        using var handler = new TimeoutHttpHandler();

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8000/"),
            Timeout = TimeSpan.FromMilliseconds(100)
        };

        var options = Microsoft.Extensions.Options.Options.Create(
            new PythonFlightSearchOptions
            {
                ApiKey = "test-internal-key"
            });

        var client = new FlightSearchClient(httpClient, options);

        var exception = await Assert.ThrowsAsync<FlightSearchException>(
            () => client.SearchAsync(
                "MOW",
                "LED",
                new DateOnly(2026, 12, 10)));

        Assert.Equal(504, exception.StatusCode);
        Assert.Equal("FLIGHT_SEARCH_TIMEOUT", exception.ErrorCode);
    }

    private sealed class TimeoutHttpHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(
                TimeSpan.FromSeconds(10),
                cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class InvalidJsonHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "{ invalid json",
                        Encoding.UTF8,
                        "application/json")
                });
        }
    }


    private sealed class RateLimitHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                new HttpResponseMessage(
                    HttpStatusCode.TooManyRequests));
        }
    }
    private sealed class ErrorHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                new HttpResponseMessage(
                    System.Net.HttpStatusCode.InternalServerError));
        }
    }


    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }

        public string? RequestUri { get; private set; }

        public string? ApiKey { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri?.PathAndQuery;

            if (request.Headers.TryGetValues(
                "X-Internal-Api-Key", out var values))
            {
                ApiKey = values.SingleOrDefault();
            }

            var json = """
            {
              "source": "Aviasales",
              "dataType": "cached_offers",
              "currency": "RUB",
              "flights": []
            }
            """;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json")
            });
        }
    }
}
