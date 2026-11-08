
using Microsoft.EntityFrameworkCore;
using TripService.Domain.Entities;

namespace TripService.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Trip> Trips => Set<Trip>();

    public DbSet<TripMember> TripMembers => Set<TripMember>();

    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<ExpenseSplit> ExpenseSplits => Set<ExpenseSplit>();

    public DbSet<TripActivity> TripActivities => Set<TripActivity>();

    public DbSet<Log> Logs => Set<Log>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);
    }
}
