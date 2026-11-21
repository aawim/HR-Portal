using HRM.ActivityLogs.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.ActivityLogs.Persistence.Configurations;

public sealed class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLogs", ActivityDbContext.DefaultSchema);

        builder.HasKey(x => x.ActivityLogId);
        builder.Property(x => x.ActivityLogId).ValueGeneratedOnAdd();

        builder.Property(x => x.EventId).IsRequired();
        builder.Property(x => x.ActorType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ActivityCategory).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ActivityType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Module).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.TargetEntityType).HasMaxLength(128);
        builder.Property(x => x.TargetEntityId).HasMaxLength(128);
        builder.Property(x => x.TargetDisplayName).HasMaxLength(200);
        builder.Property(x => x.CorrelationId).HasMaxLength(128);
        builder.Property(x => x.SessionId).HasMaxLength(64);
        builder.Property(x => x.Source).HasMaxLength(64).IsRequired();
        builder.Property(x => x.OccurredAtUtc).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => x.EventId).IsUnique().HasDatabaseName("UX_ActivityLogs_EventId");
        builder.HasIndex(x => new { x.TenantId, x.OrganisationId, x.OccurredAtUtc }).HasDatabaseName("IX_ActivityLogs_Tenant_Org_OccurredAtUtc");
        builder.HasIndex(x => new { x.OrganisationId, x.UserId, x.OccurredAtUtc }).HasDatabaseName("IX_ActivityLogs_Org_User_OccurredAtUtc");
        builder.HasIndex(x => new { x.OrganisationId, x.DivisionId, x.OccurredAtUtc }).HasDatabaseName("IX_ActivityLogs_Org_Division_OccurredAtUtc");
        builder.HasIndex(x => new { x.OrganisationId, x.TeamId, x.OccurredAtUtc }).HasDatabaseName("IX_ActivityLogs_Org_Team_OccurredAtUtc");
        builder.HasIndex(x => new { x.OrganisationId, x.ActivityCategory, x.ActivityType, x.OccurredAtUtc }).HasDatabaseName("IX_ActivityLogs_Org_Category_Type_OccurredAtUtc");
        builder.HasIndex(x => new { x.OrganisationId, x.TargetEntityType, x.TargetEntityId, x.OccurredAtUtc }).HasDatabaseName("IX_ActivityLogs_Org_Target_OccurredAtUtc");
        builder.HasIndex(x => x.CorrelationId).HasDatabaseName("IX_ActivityLogs_CorrelationId");
        builder.HasIndex(x => x.OccurredAtUtc).HasDatabaseName("IX_ActivityLogs_OccurredAtUtc");
    }
}

public sealed class ActivityOutboxMessageConfiguration : IEntityTypeConfiguration<ActivityOutboxMessage>
{
    public void Configure(EntityTypeBuilder<ActivityOutboxMessage> builder)
    {
        builder.ToTable("ActivityOutbox", ActivityDbContext.DefaultSchema);

        builder.HasKey(x => x.OutboxId);
        builder.Property(x => x.OutboxId).ValueGeneratedOnAdd();

        builder.Property(x => x.EventId).IsRequired();
        builder.Property(x => x.PayloadJson).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(1000);
        builder.Property(x => x.NextAttemptAtUtc).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => x.EventId).IsUnique().HasDatabaseName("UX_ActivityOutbox_EventId");
        builder.HasIndex(x => new { x.ProcessedAtUtc, x.NextAttemptAtUtc }).HasDatabaseName("IX_ActivityOutbox_Pending");
    }
}
