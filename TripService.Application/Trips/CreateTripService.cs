using TripService.Domain.Entities;

namespace TripService.Application.Trips;

public sealed class CreateTripService
{
    public Trip PrepareTrip(
        string? name,
        DateOnly? startDate,
        DateOnly? endDate,
        string? coverUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Trip name is required.", nameof(name));
        }

        string trimmedName = name.Trim();
        if (trimmedName.Length > 100)
        {
            throw new ArgumentException("Trip name is too long.", nameof(name));
        }

        if (startDate is null || endDate is null ||
            startDate.Value == default || endDate.Value == default ||
            endDate.Value < startDate.Value)
        {
            throw new ArgumentException("Trip dates are invalid.");
        }

        if (coverUrl is not null &&
            (coverUrl.Length > 2048 ||
             !Uri.TryCreate(coverUrl, UriKind.Absolute, out Uri? parsedUrl) ||
             parsedUrl.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Cover URL must be an HTTPS URL.", nameof(coverUrl));
        }

        return new Trip
        {
            Name = trimmedName,
            StartDate = startDate.Value,
            EndDate = endDate.Value,
            CoverUrl = coverUrl
        };
    }
}
