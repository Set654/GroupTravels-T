namespace TripService.Application.Trips;

public sealed record TripInfo(int TripId, string TripName, DateOnly StartDate,
    DateOnly EndDate, string? CoverUrl, string Status);
public sealed record TripAccess(int TripId, int MemberId, string Role);
public sealed record MemberInfo(int MemberId, int? UserId, string DisplayName,
    bool IsVirtual, string Role, string Status, string? TbankPaymentUrl);

public interface ITripRepository
{
    Task<int> CreateAsync(int userId, string name, DateOnly start, DateOnly end,
        string? coverUrl, CancellationToken ct);
    Task<IReadOnlyList<TripInfo>> ListAsync(int userId, DateOnly today, CancellationToken ct);


    Task<TripInfo?> GetAsync(int userId, int tripId, DateOnly today, CancellationToken ct);


    Task<TripAccess?> GetAccessAsync(int userId, int tripId, CancellationToken ct);
    Task<IReadOnlyList<MemberInfo>> GetMembersAsync(int tripId, CancellationToken ct);
    Task<MemberInfo> AddVirtualAsync(int tripId, string virtualName, CancellationToken ct);
}
