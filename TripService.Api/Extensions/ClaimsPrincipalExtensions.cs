using System.Security.Claims;

namespace TripService.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(
        this ClaimsPrincipal user,
        out int userId)
    {
        var value = user.FindFirst("sub")?.Value;

        return int.TryParse(value, out userId)
            && userId > 0;
    }
}