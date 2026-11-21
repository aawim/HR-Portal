using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.ActivityLogs.Persistence.Migrations;

[DbContext(typeof(ActivityDbContext))]
[Migration("20261121110000_InitialActivityLogsSchema")]
public partial class InitialActivityLogsSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "activity");

        migrationBuilder.CreateTable(
            name: "ActivityLogs",
            schema: "activity",
            columns: table => new
            {
                ActivityLogId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<int>(type: "int", nullable: true),
                OrganisationId = table.Column<int>(type: "int", nullable: true),
                DivisionId = table.Column<int>(type: "int", nullable: true),
                TeamId = table.Column<int>(type: "int", nullable: true),
                UserId = table.Column<int>(type: "int", nullable: true),
                ActorType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                ActivityCategory = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                ActivityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Module = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                TargetEntityType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                TargetEntityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                TargetDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                SessionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ActivityLogs", x => x.ActivityLogId);
            });

        migrationBuilder.CreateTable(
            name: "ActivityOutbox",
            schema: "activity",
            columns: table => new
            {
                OutboxId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                AttemptCount = table.Column<int>(type: "int", nullable: false),
                NextAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ActivityOutbox", x => x.OutboxId);
            });

        migrationBuilder.CreateIndex("UX_ActivityLogs_EventId", "ActivityLogs", "EventId", "activity", unique: true);
        migrationBuilder.CreateIndex("IX_ActivityLogs_Tenant_Org_OccurredAtUtc", "ActivityLogs", new[] { "TenantId", "OrganisationId", "OccurredAtUtc" }, "activity");
        migrationBuilder.CreateIndex("IX_ActivityLogs_Org_User_OccurredAtUtc", "ActivityLogs", new[] { "OrganisationId", "UserId", "OccurredAtUtc" }, "activity");
        migrationBuilder.CreateIndex("IX_ActivityLogs_Org_Division_OccurredAtUtc", "ActivityLogs", new[] { "OrganisationId", "DivisionId", "OccurredAtUtc" }, "activity");
        migrationBuilder.CreateIndex("IX_ActivityLogs_Org_Team_OccurredAtUtc", "ActivityLogs", new[] { "OrganisationId", "TeamId", "OccurredAtUtc" }, "activity");
        migrationBuilder.CreateIndex("IX_ActivityLogs_Org_Category_Type_OccurredAtUtc", "ActivityLogs", new[] { "OrganisationId", "ActivityCategory", "ActivityType", "OccurredAtUtc" }, "activity");
        migrationBuilder.CreateIndex("IX_ActivityLogs_Org_Target_OccurredAtUtc", "ActivityLogs", new[] { "OrganisationId", "TargetEntityType", "TargetEntityId", "OccurredAtUtc" }, "activity");
        migrationBuilder.CreateIndex("IX_ActivityLogs_CorrelationId", "ActivityLogs", "CorrelationId", "activity");
        migrationBuilder.CreateIndex("IX_ActivityLogs_OccurredAtUtc", "ActivityLogs", "OccurredAtUtc", "activity");

        migrationBuilder.CreateIndex("UX_ActivityOutbox_EventId", "ActivityOutbox", "EventId", "activity", unique: true);
        migrationBuilder.CreateIndex("IX_ActivityOutbox_Pending", "ActivityOutbox", new[] { "ProcessedAtUtc", "NextAttemptAtUtc" }, "activity");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ActivityOutbox", schema: "activity");
        migrationBuilder.DropTable(name: "ActivityLogs", schema: "activity");
    }
}
