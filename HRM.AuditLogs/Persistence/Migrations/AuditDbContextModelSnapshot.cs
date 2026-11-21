using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace HRM.AuditLogs.Persistence.Migrations;

[DbContext(typeof(AuditDbContext))]
partial class AuditDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasDefaultSchema("audit")
            .HasAnnotation("ProductVersion", "8.0.27")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("HRM.AuditLogs.Models.AuditLog", b =>
        {
            b.Property<long>("AuditLogId").ValueGeneratedOnAdd().HasColumnType("bigint");
            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("AuditLogId"));
            b.Property<string>("Action").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("ActorType").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<DateTime?>("ArchivedAtUtc").HasColumnType("datetime2");
            b.Property<string>("CorrelationId").HasMaxLength(128).HasColumnType("nvarchar(128)");
            b.Property<string>("Description").IsRequired().HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<int?>("DivisionId").HasColumnType("int");
            b.Property<string>("EntityId").IsRequired().HasMaxLength(128).HasColumnType("nvarchar(128)");
            b.Property<string>("EntityType").IsRequired().HasMaxLength(128).HasColumnType("nvarchar(128)");
            b.Property<Guid>("EventId").HasColumnType("uniqueidentifier");
            b.Property<string>("IpAddress").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<bool>("IsLegalHold").ValueGeneratedOnAdd().HasColumnType("bit").HasDefaultValue(false);
            b.Property<string>("MetadataJson").HasColumnType("nvarchar(max)");
            b.Property<string>("Module").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTime>("OccurredAtUtc").HasColumnType("datetime2");
            b.Property<int?>("OrganisationId").HasColumnType("int");
            b.Property<string>("Outcome").IsRequired().ValueGeneratedOnAdd().HasMaxLength(32).HasColumnType("nvarchar(32)").HasDefaultValue("Committed");
            b.Property<string>("Reason").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<string>("RequestId").HasMaxLength(128).HasColumnType("nvarchar(128)");
            b.Property<DateTime?>("RetentionUntilUtc").HasColumnType("datetime2");
            b.Property<int>("SchemaVersion").ValueGeneratedOnAdd().HasColumnType("int").HasDefaultValue(1);
            b.Property<string>("Source").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<int?>("TenantId").HasColumnType("int");
            b.Property<int?>("UserId").HasColumnType("int");

            b.HasKey("AuditLogId");
            b.HasIndex("CorrelationId").HasDatabaseName("IX_AuditLogs_CorrelationId");
            b.HasIndex("EventId").IsUnique().HasDatabaseName("UX_AuditLogs_EventId");
            b.HasIndex("OccurredAtUtc").HasDatabaseName("IX_AuditLogs_OccurredAtUtc");
            b.HasIndex("OrganisationId", "DivisionId", "OccurredAtUtc").HasDatabaseName("IX_AuditLogs_Org_Division_OccurredAtUtc");
            b.HasIndex("OrganisationId", "EntityType", "EntityId", "OccurredAtUtc").HasDatabaseName("IX_AuditLogs_Org_Entity_OccurredAtUtc");
            b.HasIndex("OrganisationId", "Module", "Action", "OccurredAtUtc").HasDatabaseName("IX_AuditLogs_Org_Module_Action_OccurredAtUtc");
            b.HasIndex("OrganisationId", "UserId", "OccurredAtUtc").HasDatabaseName("IX_AuditLogs_Org_User_OccurredAtUtc");
            b.HasIndex("TenantId", "OrganisationId", "OccurredAtUtc").HasDatabaseName("IX_AuditLogs_Tenant_Org_OccurredAtUtc");
            b.ToTable("AuditLogs", "audit");
        });

        modelBuilder.Entity("HRM.AuditLogs.Models.AuditLogDetail", b =>
        {
            b.Property<long>("AuditLogDetailId").ValueGeneratedOnAdd().HasColumnType("bigint");
            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("AuditLogDetailId"));
            b.Property<long>("AuditLogId").HasColumnType("bigint");
            b.Property<bool>("IsRedacted").ValueGeneratedOnAdd().HasColumnType("bit").HasDefaultValue(false);
            b.Property<string>("NewValue").HasColumnType("nvarchar(max)");
            b.Property<string>("OldValue").HasColumnType("nvarchar(max)");
            b.Property<string>("PropertyName").IsRequired().HasMaxLength(128).HasColumnType("nvarchar(128)");
            b.Property<string>("ValueType").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");

            b.HasKey("AuditLogDetailId");
            b.HasIndex("AuditLogId").HasDatabaseName("IX_AuditLogDetails_AuditLogId");
            b.HasIndex("AuditLogId", "PropertyName").HasDatabaseName("IX_AuditLogDetails_AuditLogId_PropertyName");
            b.ToTable("AuditLogDetails", "audit");
        });

        modelBuilder.Entity("HRM.AuditLogs.Models.AuditLogDetail", b =>
        {
            b.HasOne("HRM.AuditLogs.Models.AuditLog", "AuditLog")
                .WithMany("Details")
                .HasForeignKey("AuditLogId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("FK_AuditLogDetails_AuditLogs");

            b.Navigation("AuditLog");
        });

        modelBuilder.Entity("HRM.AuditLogs.Models.AuditLog", b =>
        {
            b.Navigation("Details");
        });
    }
}
