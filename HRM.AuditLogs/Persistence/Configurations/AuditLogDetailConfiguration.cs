using HRM.AuditLogs.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.AuditLogs.Persistence.Configurations;

public sealed class AuditLogDetailConfiguration : IEntityTypeConfiguration<AuditLogDetail>
{
    public void Configure(EntityTypeBuilder<AuditLogDetail> builder)
    {
        builder.ToTable("AuditLogDetails", AuditDbContext.DefaultSchema);

        builder.HasKey(x => x.AuditLogDetailId);
        builder.Property(x => x.AuditLogDetailId).ValueGeneratedOnAdd();

        builder.Property(x => x.PropertyName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ValueType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.IsRedacted).IsRequired().HasDefaultValue(false);

        // Restrict delete: never cascade-delete audit detail history.
        builder.HasOne(x => x.AuditLog)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.AuditLogId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_AuditLogDetails_AuditLogs");

        builder.HasIndex(x => x.AuditLogId).HasDatabaseName("IX_AuditLogDetails_AuditLogId");
        builder.HasIndex(x => new { x.AuditLogId, x.PropertyName }).HasDatabaseName("IX_AuditLogDetails_AuditLogId_PropertyName");
    }
}
