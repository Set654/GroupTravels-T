using System.Globalization;
using TTravel.Flights;

var baseUrl = Environment.GetEnvironmentVariable("FLIGHTS_BASE_URL") ?? "http://127.0.0.1:8000/";
var key = Environment.GetEnvironmentVariable("INTERNAL_API_KEY")
    ?? throw new InvalidOperationException("Set INTERNAL_API_KEY in the environment.");
var date = args.Length > 0
    ? DateOnly.ParseExact(args[0], "yyyy-MM-dd", CultureInfo.InvariantCulture)
    : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
using var handler = new HttpClientHandler { AllowAutoRedirect = false };
using var http = new HttpClient(handler) { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(30) };
var client = new FlightSearchClient(http, key);
try
{
    var result = await client.SearchAsync("MOW", "LED", date);
    Console.WriteLine($"Source: {result.Source}; offers: {result.Flights.Count}; currency: {result.Currency}");
    return 0;
}
catch (FlightSearchException error)
{
    Console.Error.WriteLine($"HTTP {(int)error.Status}: {error.Code}");
    return 1;
}
