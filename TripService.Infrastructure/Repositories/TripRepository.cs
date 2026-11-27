using Microsoft.EntityFrameworkCore;
using TripService.Application.Trips;
using TripService.Domain.Entities;
using TripService.Infrastructure.Persistence;
using Npgsql;

namespace TripService.Infrastructure.Repositories;

public class TripRepository : ITripRepository
{
    private readonly AppDbContext _context;

    public TripRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateAsync(
    int userId,
    string name,
    DateOnly start,
    DateOnly end,
    string? coverUrl,
    CancellationToken ct)
    {
        var trip = new Trip
        {
            Name = name,
            StartDate = start,
            EndDate = end,
            CoverUrl = coverUrl
        };

        var owner = new TripMember
        {
            Trip = trip,
            UserId = userId,
            VirtualName = null,
            Role = "Owner",
            Status = "Accepted"
        };

        _context.TripMembers.Add(owner);

        await _context.SaveChangesAsync(ct);

        return trip.Id;
    }
    public async Task<IReadOnlyList<TripInfo>> ListAsync(
    int userId,
    DateOnly today,
    CancellationToken ct)
    {
        var trips = await _context.TripMembers
            .AsNoTracking()
            .Where(m =>
                m.UserId == userId &&
                m.Status == "Accepted")
            .Select(m => m.Trip)
            .OrderBy(t => t.StartDate)
            .ToListAsync(ct);

        return trips.Select(t => new TripInfo(
            t.Id,
            t.Name,
            t.StartDate,
            t.EndDate,
            t.CoverUrl,
            t.EndDate < today
                ? "Completed"
                : t.StartDate > today
                    ? "Upcoming"
                    : "Active"
        )).ToList();
    }
    public async Task<TripInfo?> GetAsync(
    int userId,
    int tripId,
    DateOnly today,
    CancellationToken ct)
    {
        var trip = await _context.TripMembers
            .AsNoTracking()
            .Where(m =>
                m.UserId == userId &&
                m.TripId == tripId &&
                m.Status == "Accepted")
            .Select(m => m.Trip)
            .FirstOrDefaultAsync(ct);

        if (trip == null)
            return null;

        string status;

        if (trip.EndDate < today)
        {
            status = "Completed";
        }
        else if (trip.StartDate > today)
        {
            status = "Upcoming";
        }
        else
        {
            status = "Active";
        }

        return new TripInfo(
            trip.Id,
            trip.Name,
            trip.StartDate,
            trip.EndDate,
            trip.CoverUrl,
            status
        );
    }
    public async Task<TripAccess?> GetAccessAsync(
    int userId,
    int tripId,
    CancellationToken ct)
    {
        return await _context.TripMembers
            .AsNoTracking()
            .Where(m =>
                m.UserId == userId &&
                m.TripId == tripId &&
                m.Status == "Accepted")
            .Select(m => new TripAccess(
                m.TripId,
                m.Id,
                m.Role
            ))
            .FirstOrDefaultAsync(ct);
    }
    public async Task<IReadOnlyList<MemberInfo>> GetMembersAsync(
    int tripId,
    CancellationToken ct)
    {
        return await _context.TripMembers
            .AsNoTracking()
            .Where(m => m.TripId == tripId)
            .OrderBy(m => m.Id)
            .Select(m => new MemberInfo(
                m.Id,
                m.UserId,
                m.UserId == null
                    ? m.VirtualName!
                    : m.User!.Name,
                m.UserId == null,
                m.Role,
                m.Status,
                m.UserId == null
                    ? null
                    : m.User!.TbankPaymentUrl
            ))
            .ToListAsync(ct);
    }
    public async Task<MemberInfo> AddVirtualAsync(
    int tripId,
    string virtualName,
    CancellationToken ct)
    {
        var member = new TripMember
        {
            TripId = tripId,
            UserId = null,
            VirtualName = virtualName,
            Role = "Member",
            Status = "Accepted"
        };

        _context.TripMembers.Add(member);

        await _context.SaveChangesAsync(ct);

        return new MemberInfo(
            member.Id,
            member.UserId,
            member.VirtualName!,
            true,
            member.Role,
            member.Status,
            null
        );
    }

    public async Task<InviteResult> InviteAsync(
    int tripId,
    string? email,
    string? phone,
    CancellationToken ct)
    {
        User? user;

        if (email is not null)
        {
            user = await _context.Users
                .SingleOrDefaultAsync(u => u.Email == email, ct);
        }
        else
        {
            user = await _context.Users
                .SingleOrDefaultAsync(u => u.Phone == phone, ct);
        }

        if (user is null)
        {
            return new InviteResult(InviteStatus.UserNotFound);
        }

        var alreadyExists = await _context.TripMembers
            .AnyAsync(m =>
                m.TripId == tripId &&
                m.UserId == user.Id,
                ct);

        if (alreadyExists)
        {
            return new InviteResult(InviteStatus.AlreadyInvited);
        }

        var member = new TripMember
        {
            TripId = tripId,
            UserId = user.Id,
            VirtualName = null,
            Role = "Member",
            Status = "Pending"
        };

        _context.TripMembers.Add(member);

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException pg &&
            pg.SqlState == PostgresErrorCodes.UniqueViolation &&
            pg.ConstraintName == "IX_trip_members_trip_id_user_id")
        {
            _context.Entry(member).State = EntityState.Detached;

            return new InviteResult(InviteStatus.AlreadyInvited);
        }

        return new InviteResult(
            InviteStatus.Created,
            member.Id);
    }

    public async Task<IReadOnlyList<InvitationInfo>> GetInvitationsAsync(
    int userId,
    CancellationToken ct)
    {
        var query =
            from member in _context.TripMembers.AsNoTracking()
            join owner in _context.TripMembers.AsNoTracking()
                on member.TripId equals owner.TripId
            where member.UserId == userId
                && member.Status == "Pending"
                && owner.Role == "Owner"
                && owner.Status == "Accepted"
            select new InvitationInfo(
                member.Id,
                member.TripId,
                member.Trip.Name,
                owner.User!.Name,
                member.Trip.StartDate,
                member.Trip.EndDate);

        return await query.ToListAsync(ct);
    }

    public async Task<bool> RespondToInviteAsync(
    int userId,
    int tripId,
    int memberId,
    string status,
    CancellationToken ct)
    {
        var affectedRows = await _context.TripMembers
            .Where(m =>
                m.Id == memberId &&
                m.TripId == tripId &&
                m.UserId == userId &&
                m.Status == "Pending")
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    m => m.Status,
                    status),
                ct);

        return affectedRows == 1;
    }
}