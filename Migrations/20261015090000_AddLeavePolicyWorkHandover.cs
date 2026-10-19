using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HRM.Migrations;

[DbContext(typeof(HrmTeContext))]
[Migration("20261015090000_AddLeavePolicyWorkHandover")]
public class AddLeavePolicyWorkHandover : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Also supports databases updated using the matching deployment script.
        migrationBuilder.Sql("""
            IF COL_LENGTH('dbo.LeavePolicies', 'RequireWorkHandover') IS NULL
                ALTER TABLE dbo.LeavePolicies ADD RequireWorkHandover bit NOT NULL CONSTRAINT DF_LeavePolicies_RequireWorkHandover DEFAULT (0);
            IF COL_LENGTH('dbo.LeavePolicies', 'WorkHandoverAfterDays') IS NULL
                ALTER TABLE dbo.LeavePolicies ADD WorkHandoverAfterDays int NOT NULL CONSTRAINT DF_LeavePolicies_WorkHandoverAfterDays DEFAULT (2);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("RequireWorkHandover", "LeavePolicies");
        migrationBuilder.DropColumn("WorkHandoverAfterDays", "LeavePolicies");
    }
}
