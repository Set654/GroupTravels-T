
using System.Globalization;
using System.Net.Http;
using TripService.Application.Flights.DTOs;
using TripService.Application.Flights.Interfaces;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using TripService.Infrastructure.Configuration;


namespace TripService.Infrastructure.Flights;

public sealed class FlightSearchClient : IFlightSearchClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public FlightSearchClient(
        HttpClient httpClient,
        IOptions<PythonFlightSearchOptions> options)
    {
        _httpClient = httpClient;
        _apiKey = !string.IsNullOrWhiteSpace(options.Value.ApiKey)
        ? options.Value.ApiKey
            : throw new ArgumentException(
             "Не настроен API-ключ Python-сервиса.");
    }

    public async Task<FlightSearchResponseDto> SearchAsync(
        string origin,
        string destination,
        DateOnly departureDate,
        CancellationToken cancellationToken = default)
    {
        string date = departureDate.ToString(
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);

        string url =
            $"/internal/flights/search" +
            $"?origin={Uri.EscapeDataString(origin)}" +
            $"&destination={Uri.EscapeDataString(destination)}" +
            $"&departureDate={date}";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            url);
        request.Headers.Add(
            "X-Internal-Api-Key",
            _apiKey);


        HttpResponseMessage response;

        try
        {
            response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }
        catch (OperationCanceledException ex)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new FlightSearchException(
                504,
                "FLIGHT_SEARCH_TIMEOUT",
                "Python-сервис не ответил вовремя.",
                ex);
        }
        catch (HttpRequestException ex)
        {
            throw new FlightSearchException(
                502,
                "FLIGHT_PROVIDER_UNAVAILABLE",
                "Не удалось подключиться к Python-сервису.",
                ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                int statusCode = (int)response.StatusCode;

                if (statusCode == 400)
                {
                    throw new FlightSearchException(
                        400,
                        "INVALID_FLIGHT_SEARCH",
                        "Python-сервис отклонил параметры поиска.");
                }

                if (statusCode == 429)
                {
                    throw new FlightSearchException(
                        429,
                        "FLIGHT_SEARCH_RATE_LIMITED",
                        "Превышен лимит запросов к сервису авиапоиска.");
                }

                throw new FlightSearchException(
                    502,
                    "FLIGHT_PROVIDER_ERROR",
                    $"Ошибка Python-сервиса. HTTP {statusCode}.");
            }

            try
            {
                var flightResponse = await response.Content
                    .ReadFromJsonAsync<FlightSearchResponseDto>(
                         cancellationToken: cancellationToken);

                if (flightResponse is null)
                {
                    throw new FlightSearchException(
                        502,
                        "FLIGHT_PROVIDER_INVALID_RESPONSE",
                        "Python-сервис вернул пустой JSON.");
                }

                return flightResponse;
            }
            catch (System.Text.Json.JsonException ex)
            {
                throw new FlightSearchException(
                    502,
                    "FLIGHT_PROVIDER_INVALID_RESPONSE",
                    "Python-сервис вернул некорректный JSON.",
                    ex);
            }
            catch (OperationCanceledException ex)
                when (!cancellationToken.IsCancellationRequested)
            {
                throw new FlightSearchException(
                    504,
                    "FLIGHT_SEARCH_TIMEOUT",
                    "Превышено время чтения ответа Python-сервиса.",
                    ex);
            }
        }
    }
}
