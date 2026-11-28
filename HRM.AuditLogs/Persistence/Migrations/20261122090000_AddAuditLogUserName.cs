using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.AuditLogs.Persistence.Migrations;

[DbContext(typeof(AuditDbContext))]
[Migration("20261122090000_AddAuditLogUserName")]
public partial class AddAuditLogUserName : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF COL_LENGTH(N'[audit].[AuditLogs]', N'UserName') IS NULL
    ALTER TABLE [audit].[AuditLogs] ADD [UserName] nvarchar(200) NULL;
IF COL_LENGTH(N'[audit].[AuditLogs]', N'Url') IS NULL
    ALTER TABLE [audit].[AuditLogs] ADD [Url] nvarchar(500) NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Url",
            schema: "audit",
            table: "AuditLogs");

        migrationBuilder.DropColumn(
            name: "UserName",
            schema: "audit",
            table: "AuditLogs");
    }
}
