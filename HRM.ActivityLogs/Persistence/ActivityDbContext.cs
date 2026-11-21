using HRM.ActivityLogs.Models;
using HRM.ActivityLogs.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace HRM.ActivityLogs.Persistence;

public sealed class ActivityDbContext : DbContext
{
    public const string DefaultSchema = "activity";

    public ActivityDbContext(DbContextOptions<ActivityDbContext> options)
        : base(options)
    {
    }

    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<ActivityOutboxMessage> ActivityOutbox => Set<ActivityOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfiguration(new ActivityLogConfiguration());
        modelBuilder.ApplyConfiguration(new ActivityOutboxMessageConfiguration());
    }
}
