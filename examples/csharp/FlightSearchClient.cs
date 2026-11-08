using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TTravel.Flights;

public sealed record FlightOffer(
    string Origin, string Destination, DateOnly DepartureDate,
    DateTimeOffset? DepartureAt, string? Airline, string? FlightNumber,
    decimal Price, string Currency, int? Transfers, int? DurationMinutes, Uri? BookingUrl);

public sealed record FlightSearchResult(
    string Source, string DataType, string Currency, List<FlightOffer> Flights);

public sealed record FlightSearchError(string Code, string Message);

public sealed class FlightSearchException(HttpStatusCode status, FlightSearchError error)
    : Exception(error.Message)
{
    public HttpStatusCode Status { get; } = status;
    public string Code { get; } = error.Code;
}

// Example adapter only. User/trip authorization and persistence belong to the main API.
public sealed class FlightSearchClient(HttpClient http, string internalApiKey)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> AllowedHosts =
        ["aviasales.ru", "www.aviasales.ru", "aviasales.com", "www.aviasales.com"];

    public async Task<FlightSearchResult> SearchAsync(
        string origin, string destination, DateOnly departureDate, CancellationToken ct = default)
    {
        if (http.BaseAddress is null)
            throw new InvalidOperationException("Configure the Python service BaseAddress.");
        if (string.IsNullOrWhiteSpace(internalApiKey))
            throw new InvalidOperationException("Configure the internal service key.");
        origin = origin.Trim().ToUpperInvariant();
        destination = destination.Trim().ToUpperInvariant();
        var date = departureDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var path = $"internal/flights/search?origin={Uri.EscapeDataString(origin)}" +
                   $"&destination={Uri.EscapeDataString(destination)}&departureDate={date}";
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Internal-Api-Key", internalApiKey);
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            FlightSearchError? error;
            try
            {
                error = await response.Content.ReadFromJsonAsync<FlightSearchError>(JsonOptions, ct);
            }
            catch (JsonException)
            {
                error = null; // A proxy can return HTML instead of our JSON envelope.
            }
            throw new FlightSearchException(response.StatusCode,
                error ?? new("FLIGHT_SERVICE_UNAVAILABLE", "The flight service returned an invalid error."));
        }
        FlightSearchResult? result;
        try
        {
            result = await response.Content.ReadFromJsonAsync<FlightSearchResult>(JsonOptions, ct);
        }
        catch (JsonException)
        {
            throw InvalidResponse();
        }
        if (result is null || result.Source != "Aviasales" || result.DataType != "cached_offers"
            || result.Currency != "RUB" || result.Flights is null || result.Flights.Count > 10)
            throw InvalidResponse();
        decimal previousPrice = 0;
        foreach (var flight in result.Flights)
        {
            if (flight is null || flight.Origin != origin || flight.Destination != destination
                || flight.DepartureDate != departureDate || flight.Price <= 0
                || flight.Price < previousPrice || flight.Currency != "RUB"
                || flight.Transfers is < 0 || flight.DurationMinutes is < 0)
                throw InvalidResponse();
            if (flight.BookingUrl is { } link && (!link.IsAbsoluteUri || link.Scheme != "https"
                || !AllowedHosts.Contains(link.Host) || !link.IsDefaultPort
                || link.UserInfo.Length != 0 || !link.AbsolutePath.StartsWith("/search/", StringComparison.Ordinal)))
                throw InvalidResponse();
            previousPrice = flight.Price;
        }
        return result;
    }

    private static FlightSearchException InvalidResponse() => new(
        HttpStatusCode.BadGateway, new("FLIGHT_PROVIDER_ERROR", "The flight service returned invalid data."));
}
