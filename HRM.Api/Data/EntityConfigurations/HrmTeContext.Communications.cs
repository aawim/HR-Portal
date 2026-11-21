using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public partial class HrmTeContext
{
    public DbSet<CommunicationOutbox> CommunicationOutbox => Set<CommunicationOutbox>();
    public DbSet<CommunicationProviderConnection> CommunicationProviderConnections => Set<CommunicationProviderConnection>();
    public DbSet<CommunicationDestination> CommunicationDestinations => Set<CommunicationDestination>();
    public DbSet<CommunicationAutomationRule> CommunicationAutomationRules => Set<CommunicationAutomationRule>();
    public DbSet<CommunicationMessage> CommunicationMessages => Set<CommunicationMessage>();
    public DbSet<CommunicationDelivery> CommunicationDeliveries => Set<CommunicationDelivery>();
    public DbSet<CommunicationDeliveryAttempt> CommunicationDeliveryAttempts => Set<CommunicationDeliveryAttempt>();
}

public sealed class CommunicationMessageMapping : IEntityTypeConfiguration<CommunicationMessage>
{
    public void Configure(EntityTypeBuilder<CommunicationMessage> b)
    {
        b.ToTable("CM_Messages"); b.HasKey(x => x.CommunicationMessageId);
        b.Property(x => x.Subject).HasMaxLength(300).IsRequired(); b.Property(x => x.RenderedBody).HasMaxLength(4000).IsRequired();
        b.HasIndex(x => new { x.CommunicationOutboxId, x.CommunicationAutomationRuleId }).IsUnique().HasDatabaseName("UX_CM_Messages_Outbox_Rule");
    }
}

public sealed class CommunicationDeliveryMapping : IEntityTypeConfiguration<CommunicationDelivery>
{
    public void Configure(EntityTypeBuilder<CommunicationDelivery> b)
    {
        b.ToTable("CM_Deliveries"); b.HasKey(x => x.CommunicationDeliveryId);
        b.Property(x => x.Status).HasMaxLength(16).IsRequired(); b.Property(x => x.IdempotencyKey).HasMaxLength(120).IsRequired(); b.Property(x => x.ProviderMessageId).HasMaxLength(256); b.Property(x => x.LastErrorCode).HasMaxLength(128);
        b.HasIndex(x => x.IdempotencyKey).IsUnique(); b.HasIndex(x => new { x.Status, x.NextRetryAtUtc });
    }
}

public sealed class CommunicationDeliveryAttemptMapping : IEntityTypeConfiguration<CommunicationDeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<CommunicationDeliveryAttempt> b)
    {
        b.ToTable("CM_DeliveryAttempts"); b.HasKey(x => x.CommunicationDeliveryAttemptId);
        b.Property(x => x.ResultStatus).HasMaxLength(16).IsRequired(); b.Property(x => x.SanitizedErrorCode).HasMaxLength(128);
        b.HasIndex(x => new { x.CommunicationDeliveryId, x.AttemptNumber }).IsUnique();
    }
}

public sealed class CommunicationOutboxMapping : IEntityTypeConfiguration<CommunicationOutbox>
{
    public void Configure(EntityTypeBuilder<CommunicationOutbox> b)
    {
        b.ToTable("CM_CommunicationOutbox"); b.HasKey(x => x.CommunicationOutboxId);
        b.Property(x => x.EventCode).HasMaxLength(120).IsRequired(); b.Property(x => x.AggregateType).HasMaxLength(80).IsRequired(); b.Property(x => x.AggregateId).HasMaxLength(80).IsRequired();
        b.HasIndex(x => x.EventId).IsUnique(); b.HasIndex(x => new { x.Status, x.AvailableAtUtc });
    }
}

public sealed class CommunicationProviderConnectionMapping : IEntityTypeConfiguration<CommunicationProviderConnection>
{
    public void Configure(EntityTypeBuilder<CommunicationProviderConnection> b)
    {
        b.ToTable("CM_ProviderConnections"); b.HasKey(x => x.CommunicationProviderConnectionId);
        b.Property(x => x.ProviderCode).HasMaxLength(32).IsRequired(); b.Property(x => x.ConnectionName).HasMaxLength(120).IsRequired(); b.Property(x => x.CredentialReference).HasMaxLength(256).IsRequired();
        b.HasIndex(x => new { x.OrganisationBusinessEntityId, x.ProviderCode, x.ConnectionName }).IsUnique();
    }
}

public sealed class CommunicationDestinationMapping : IEntityTypeConfiguration<CommunicationDestination>
{
    public void Configure(EntityTypeBuilder<CommunicationDestination> b)
    {
        b.ToTable("CM_Destinations"); b.HasKey(x => x.CommunicationDestinationId);
        b.Property(x => x.DestinationType).HasMaxLength(32).IsRequired(); b.Property(x => x.ExternalDestinationId).HasMaxLength(256).IsRequired(); b.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
        b.HasIndex(x => new { x.CommunicationProviderConnectionId, x.ExternalDestinationId }).IsUnique();
    }
}

public sealed class CommunicationAutomationRuleMapping : IEntityTypeConfiguration<CommunicationAutomationRule>
{
    public void Configure(EntityTypeBuilder<CommunicationAutomationRule> b)
    {
        b.ToTable("CM_AutomationRules"); b.HasKey(x => x.CommunicationAutomationRuleId);
        b.HasOne<CommunicationDestination>().WithMany().HasForeignKey(x => x.CommunicationDestinationId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.EventCode).HasMaxLength(120).IsRequired(); b.Property(x => x.RuleName).HasMaxLength(160).IsRequired(); b.Property(x => x.TemplateBody).HasMaxLength(4000).IsRequired();
        b.HasIndex(x => new { x.OrganisationBusinessEntityId, x.EventCode, x.IsEnabled });
    }
}
