
namespace TripService.Infrastructure.Flights;

public sealed class FlightSearchException : Exception
{
    public int StatusCode { get; }

    public string ErrorCode { get; }

    public FlightSearchException(
        int statusCode,
        string errorCode,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}
