using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.ActivityLogs.Persistence.Migrations;

[DbContext(typeof(ActivityDbContext))]
[Migration("20261122090000_AddActivityLogUserNameAndIpAddress")]
public partial class AddActivityLogUserNameAndIpAddress : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "UserName",
            schema: "activity",
            table: "ActivityLogs",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "IpAddress",
            schema: "activity",
            table: "ActivityLogs",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Url",
            schema: "activity",
            table: "ActivityLogs",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Url",
            schema: "activity",
            table: "ActivityLogs");

        migrationBuilder.DropColumn(
            name: "UserName",
            schema: "activity",
            table: "ActivityLogs");

        migrationBuilder.DropColumn(
            name: "IpAddress",
            schema: "activity",
            table: "ActivityLogs");
    }
}
