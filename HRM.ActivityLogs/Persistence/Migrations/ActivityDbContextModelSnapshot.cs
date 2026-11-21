using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace HRM.ActivityLogs.Persistence.Migrations;

[DbContext(typeof(ActivityDbContext))]
partial class ActivityDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasDefaultSchema("activity")
            .HasAnnotation("ProductVersion", "8.0.27")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("HRM.ActivityLogs.Models.ActivityLog", b =>
        {
            b.Property<long>("ActivityLogId").ValueGeneratedOnAdd().HasColumnType("bigint");
            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("ActivityLogId"));
            b.Property<string>("Action").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("ActivityCategory").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("ActivityType").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("ActorType").IsRequired().HasMaxLength(32).HasColumnType("nvarchar(32)");
            b.Property<string>("CorrelationId").HasMaxLength(128).HasColumnType("nvarchar(128)");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("datetime2");
            b.Property<string>("Description").IsRequired().HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<int?>("DivisionId").HasColumnType("int");
            b.Property<Guid>("EventId").HasColumnType("uniqueidentifier");
            b.Property<string>("MetadataJson").HasColumnType("nvarchar(max)");
            b.Property<string>("Module").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTime>("OccurredAtUtc").HasColumnType("datetime2");
            b.Property<int?>("OrganisationId").HasColumnType("int");
            b.Property<string>("SessionId").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("Source").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("TargetDisplayName").HasMaxLength(200).HasColumnType("nvarchar(200)");
            b.Property<string>("TargetEntityId").HasMaxLength(128).HasColumnType("nvarchar(128)");
            b.Property<string>("TargetEntityType").HasMaxLength(128).HasColumnType("nvarchar(128)");
            b.Property<int?>("TeamId").HasColumnType("int");
            b.Property<int?>("TenantId").HasColumnType("int");
            b.Property<int?>("UserId").HasColumnType("int");

            b.HasKey("ActivityLogId");
            b.HasIndex("CorrelationId").HasDatabaseName("IX_ActivityLogs_CorrelationId");
            b.HasIndex("EventId").IsUnique().HasDatabaseName("UX_ActivityLogs_EventId");
            b.HasIndex("OccurredAtUtc").HasDatabaseName("IX_ActivityLogs_OccurredAtUtc");
            b.HasIndex("OrganisationId", "ActivityCategory", "ActivityType", "OccurredAtUtc").HasDatabaseName("IX_ActivityLogs_Org_Category_Type_OccurredAtUtc");
            b.HasIndex("OrganisationId", "DivisionId", "OccurredAtUtc").HasDatabaseName("IX_ActivityLogs_Org_Division_OccurredAtUtc");
            b.HasIndex("OrganisationId", "TargetEntityType", "TargetEntityId", "OccurredAtUtc").HasDatabaseName("IX_ActivityLogs_Org_Target_OccurredAtUtc");
            b.HasIndex("OrganisationId", "TeamId", "OccurredAtUtc").HasDatabaseName("IX_ActivityLogs_Org_Team_OccurredAtUtc");
            b.HasIndex("OrganisationId", "UserId", "OccurredAtUtc").HasDatabaseName("IX_ActivityLogs_Org_User_OccurredAtUtc");
            b.HasIndex("TenantId", "OrganisationId", "OccurredAtUtc").HasDatabaseName("IX_ActivityLogs_Tenant_Org_OccurredAtUtc");
            b.ToTable("ActivityLogs", "activity");
        });

        modelBuilder.Entity("HRM.ActivityLogs.Models.ActivityOutboxMessage", b =>
        {
            b.Property<long>("OutboxId").ValueGeneratedOnAdd().HasColumnType("bigint");
            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("OutboxId"));
            b.Property<int>("AttemptCount").HasColumnType("int");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("datetime2");
            b.Property<Guid>("EventId").HasColumnType("uniqueidentifier");
            b.Property<string>("LastError").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<DateTime>("NextAttemptAtUtc").HasColumnType("datetime2");
            b.Property<string>("PayloadJson").IsRequired().HasColumnType("nvarchar(max)");
            b.Property<DateTime?>("ProcessedAtUtc").HasColumnType("datetime2");

            b.HasKey("OutboxId");
            b.HasIndex("EventId").IsUnique().HasDatabaseName("UX_ActivityOutbox_EventId");
            b.HasIndex("ProcessedAtUtc", "NextAttemptAtUtc").HasDatabaseName("IX_ActivityOutbox_Pending");
            b.ToTable("ActivityOutbox", "activity");
        });
    }
}
