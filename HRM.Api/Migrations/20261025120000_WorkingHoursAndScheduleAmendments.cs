using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HRM.Migrations;

[DbContext(typeof(HrmTeContext))]
[Migration("20261025120000_WorkingHoursAndScheduleAmendments")]
public sealed class WorkingHoursAndScheduleAmendments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        using var stream = typeof(WorkingHoursAndScheduleAmendments).Assembly.GetManifestResourceStream("HRM.Database.Scripts.20261025_WorkingHoursAndScheduleAmendments.sql")
            ?? throw new InvalidOperationException("Working-hours installation script is unavailable.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }
    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Working-hour rules and amendment history must be preserved. Roll back the application without dropping these tables.");
}
