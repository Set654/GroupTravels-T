using Microsoft.EntityFrameworkCore;
using TripService.Application.Trips;
using TripService.Domain.Entities;
using TripService.Infrastructure.Persistence;

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
}