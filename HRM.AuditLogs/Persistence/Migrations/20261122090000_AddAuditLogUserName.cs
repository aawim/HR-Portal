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
        migrationBuilder.AddColumn<string>(
            name: "UserName",
            schema: "audit",
            table: "AuditLogs",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Url",
            schema: "audit",
            table: "AuditLogs",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);
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
