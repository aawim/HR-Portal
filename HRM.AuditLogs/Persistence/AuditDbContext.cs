using HRM.AuditLogs.Models;
using HRM.AuditLogs.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace HRM.AuditLogs.Persistence;

public sealed class AuditDbContext : DbContext
{
    public const string DefaultSchema = "audit";

    public AuditDbContext(DbContextOptions<AuditDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AuditLogDetail> AuditLogDetails => Set<AuditLogDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogDetailConfiguration());
    }
}
