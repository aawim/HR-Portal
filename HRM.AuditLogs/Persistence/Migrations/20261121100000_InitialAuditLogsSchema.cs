using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.AuditLogs.Persistence.Migrations;

[DbContext(typeof(AuditDbContext))]
[Migration("20261121100000_InitialAuditLogsSchema")]
public partial class InitialAuditLogsSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "audit");

        migrationBuilder.CreateTable(
            name: "AuditLogs",
            schema: "audit",
            columns: table => new
            {
                AuditLogId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<int>(type: "int", nullable: true),
                OrganisationId = table.Column<int>(type: "int", nullable: true),
                DivisionId = table.Column<int>(type: "int", nullable: true),
                UserId = table.Column<int>(type: "int", nullable: true),
                ActorType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Outcome = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "Committed"),
                Module = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                EntityType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                EntityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                RequestId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                SchemaVersion = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                RetentionUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsLegalHold = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                ArchivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogs", x => x.AuditLogId);
            });

        migrationBuilder.CreateTable(
            name: "AuditLogDetails",
            schema: "audit",
            columns: table => new
            {
                AuditLogDetailId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                AuditLogId = table.Column<long>(type: "bigint", nullable: false),
                PropertyName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ValueType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                IsRedacted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogDetails", x => x.AuditLogDetailId);
                table.ForeignKey(
                    name: "FK_AuditLogDetails_AuditLogs",
                    column: x => x.AuditLogId,
                    principalSchema: "audit",
                    principalTable: "AuditLogs",
                    principalColumn: "AuditLogId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "UX_AuditLogs_EventId",
            schema: "audit",
            table: "AuditLogs",
            column: "EventId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_Tenant_Org_OccurredAtUtc",
            schema: "audit",
            table: "AuditLogs",
            columns: new[] { "TenantId", "OrganisationId", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_Org_Entity_OccurredAtUtc",
            schema: "audit",
            table: "AuditLogs",
            columns: new[] { "OrganisationId", "EntityType", "EntityId", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_Org_User_OccurredAtUtc",
            schema: "audit",
            table: "AuditLogs",
            columns: new[] { "OrganisationId", "UserId", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_Org_Module_Action_OccurredAtUtc",
            schema: "audit",
            table: "AuditLogs",
            columns: new[] { "OrganisationId", "Module", "Action", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_Org_Division_OccurredAtUtc",
            schema: "audit",
            table: "AuditLogs",
            columns: new[] { "OrganisationId", "DivisionId", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_CorrelationId",
            schema: "audit",
            table: "AuditLogs",
            column: "CorrelationId");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_OccurredAtUtc",
            schema: "audit",
            table: "AuditLogs",
            column: "OccurredAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogDetails_AuditLogId",
            schema: "audit",
            table: "AuditLogDetails",
            column: "AuditLogId");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogDetails_AuditLogId_PropertyName",
            schema: "audit",
            table: "AuditLogDetails",
            columns: new[] { "AuditLogId", "PropertyName" });

        // Append-only immutability protection for ordinary application accounts (SQL Server).
        migrationBuilder.Sql(@"
CREATE OR ALTER TRIGGER [audit].[TR_AuditLogs_AppendOnly]
ON [audit].[AuditLogs]
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted) AND NOT EXISTS (SELECT 1 FROM inserted)
    BEGIN
        THROW 51001, 'Audit log records are append-only and cannot be deleted.', 1;
    END

    -- Allow only legal-hold or archival metadata transitions by authorised maintenance; block content mutation.
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN deleted d ON i.AuditLogId = d.AuditLogId
        WHERE i.EventId <> d.EventId
           OR ISNULL(i.TenantId, -1) <> ISNULL(d.TenantId, -1)
           OR ISNULL(i.OrganisationId, -1) <> ISNULL(d.OrganisationId, -1)
           OR ISNULL(i.UserId, -1) <> ISNULL(d.UserId, -1)
           OR i.Action <> d.Action
           OR i.Outcome <> d.Outcome
           OR i.Module <> d.Module
           OR i.EntityType <> d.EntityType
           OR i.EntityId <> d.EntityId
           OR i.Description <> d.Description
           OR i.OccurredAtUtc <> d.OccurredAtUtc
    )
    BEGIN
        THROW 51002, 'Audit log records are immutable and cannot be modified.', 1;
    END

    UPDATE t
    SET t.IsLegalHold = i.IsLegalHold,
        t.ArchivedAtUtc = i.ArchivedAtUtc,
        t.RetentionUntilUtc = i.RetentionUntilUtc
    FROM [audit].[AuditLogs] t
    JOIN inserted i ON t.AuditLogId = i.AuditLogId;
END;
");

        migrationBuilder.Sql(@"
CREATE OR ALTER TRIGGER [audit].[TR_AuditLogDetails_AppendOnly]
ON [audit].[AuditLogDetails]
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51003, 'Audit log detail records are immutable and append-only.', 1;
END;
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF OBJECT_ID('[audit].[TR_AuditLogDetails_AppendOnly]', 'TR') IS NOT NULL DROP TRIGGER [audit].[TR_AuditLogDetails_AppendOnly];");
        migrationBuilder.Sql("IF OBJECT_ID('[audit].[TR_AuditLogs_AppendOnly]', 'TR') IS NOT NULL DROP TRIGGER [audit].[TR_AuditLogs_AppendOnly];");
        migrationBuilder.DropTable(name: "AuditLogDetails", schema: "audit");
        migrationBuilder.DropTable(name: "AuditLogs", schema: "audit");
    }
}
