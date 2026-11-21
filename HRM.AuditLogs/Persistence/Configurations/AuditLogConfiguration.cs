using HRM.AuditLogs.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.AuditLogs.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", AuditDbContext.DefaultSchema);

        builder.HasKey(x => x.AuditLogId);
        builder.Property(x => x.AuditLogId).ValueGeneratedOnAdd();

        builder.Property(x => x.EventId).IsRequired();
        builder.Property(x => x.ActorType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Outcome).HasMaxLength(32).IsRequired().HasDefaultValue("Committed");
        builder.Property(x => x.Module).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(128);
        builder.Property(x => x.RequestId).HasMaxLength(128);
        builder.Property(x => x.Source).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.OccurredAtUtc).IsRequired();
        builder.Property(x => x.SchemaVersion).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.IsLegalHold).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => x.EventId).IsUnique().HasDatabaseName("UX_AuditLogs_EventId");
        builder.HasIndex(x => new { x.TenantId, x.OrganisationId, x.OccurredAtUtc }).HasDatabaseName("IX_AuditLogs_Tenant_Org_OccurredAtUtc");
        builder.HasIndex(x => new { x.OrganisationId, x.EntityType, x.EntityId, x.OccurredAtUtc }).HasDatabaseName("IX_AuditLogs_Org_Entity_OccurredAtUtc");
        builder.HasIndex(x => new { x.OrganisationId, x.UserId, x.OccurredAtUtc }).HasDatabaseName("IX_AuditLogs_Org_User_OccurredAtUtc");
        builder.HasIndex(x => new { x.OrganisationId, x.Module, x.Action, x.OccurredAtUtc }).HasDatabaseName("IX_AuditLogs_Org_Module_Action_OccurredAtUtc");
        builder.HasIndex(x => new { x.OrganisationId, x.DivisionId, x.OccurredAtUtc }).HasDatabaseName("IX_AuditLogs_Org_Division_OccurredAtUtc");
        builder.HasIndex(x => x.CorrelationId).HasDatabaseName("IX_AuditLogs_CorrelationId");
        builder.HasIndex(x => x.OccurredAtUtc).HasDatabaseName("IX_AuditLogs_OccurredAtUtc");
    }
}
