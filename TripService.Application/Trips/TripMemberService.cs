namespace TripService.Application.Trips;

public sealed class TripMemberService
{
    private readonly ITripRepository _tripRepository;

    public TripMemberService(ITripRepository tripRepository)
    {
        _tripRepository = tripRepository;
    }

    public async Task<IReadOnlyList<MemberInfo>?> GetMembersAsync(
        int userId,
        int tripId,
        CancellationToken ct)
    {
        var access = await _tripRepository.GetAccessAsync(
            userId,
            tripId,
            ct);

        if (access is null)
        {
            return null;
        }

        return await _tripRepository.GetMembersAsync(
            tripId,
            ct);
    }

    public async Task<MemberInfo?> AddVirtualAsync(
        int userId,
        int tripId,
        string? virtualName,
        CancellationToken ct)
    {
        var access = await _tripRepository.GetAccessAsync(
            userId,
            tripId,
            ct);

        if (access is null || access.Role != "Owner")
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(virtualName))
        {
            throw new ArgumentException(
                "Введите имя виртуального участника.");
        }

        virtualName = virtualName.Trim();

        if (virtualName.Length > 100)
        {
            throw new ArgumentException(
                "Имя не должно превышать 100 символов.");
        }

        return await _tripRepository.AddVirtualAsync(
            tripId,
            virtualName,
            ct);
    }

    public async Task<InviteResult> InviteAsync(
    int userId,
    int tripId,
    string? email,
    string? phone,
    CancellationToken ct)
    {
        var access = await _tripRepository.GetAccessAsync(
            userId,
            tripId,
            ct);

        if (access is null || access.Role != "Owner")
        {
            return new InviteResult(InviteStatus.Forbidden);
        }

        email = string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim();

        phone = string.IsNullOrWhiteSpace(phone)
            ? null
            : phone.Trim();

        if ((email is null && phone is null) ||
            (email is not null && phone is not null))
        {
            throw new ArgumentException(
                "Укажите только телефон или только email.");
        }

        if (email?.Length > 150 || phone?.Length > 20)
        {
            throw new ArgumentException(
                "Некорректная длина телефона или email.");
        }

        return await _tripRepository.InviteAsync(
            tripId,
            email,
            phone,
            ct);
    }

    public Task<IReadOnlyList<InvitationInfo>> GetInvitationsAsync(
        int userId,
        CancellationToken ct)
    {
        return _tripRepository.GetInvitationsAsync(
            userId,
            ct);
    }

    public Task<bool> RespondToInviteAsync(
        int userId,
        int tripId,
        int memberId,
        string? decision,
        CancellationToken ct)
    {
        if (memberId <= 0)
        {
            throw new ArgumentException(
                "Некорректный идентификатор участника.");
        }

        string status = decision switch
        {
            "Accept" => "Accepted",
            "Decline" => "Declined",

            _ => throw new ArgumentException(
                "Допустимые решения: Accept или Decline.")
        };

        return _tripRepository.RespondToInviteAsync(
            userId,
            tripId,
            memberId,
            status,
            ct);
    }
}