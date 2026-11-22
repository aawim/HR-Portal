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
        migrationBuilder.Sql(@"
IF COL_LENGTH(N'[activity].[ActivityLogs]', N'UserName') IS NULL
    ALTER TABLE [activity].[ActivityLogs] ADD [UserName] nvarchar(200) NULL;
IF COL_LENGTH(N'[activity].[ActivityLogs]', N'IpAddress') IS NULL
    ALTER TABLE [activity].[ActivityLogs] ADD [IpAddress] nvarchar(64) NULL;
IF COL_LENGTH(N'[activity].[ActivityLogs]', N'Url') IS NULL
    ALTER TABLE [activity].[ActivityLogs] ADD [Url] nvarchar(500) NULL;");
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
