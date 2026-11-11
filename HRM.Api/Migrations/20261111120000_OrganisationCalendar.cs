using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HRM.Migrations;

[DbContext(typeof(HrmTeContext))]
[Migration("20261111120000_OrganisationCalendar")]
public sealed class OrganisationCalendar : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        using var stream = typeof(OrganisationCalendar).Assembly.GetManifestResourceStream("HRM.Database.Scripts.20261111_OrganisationCalendar.sql")
            ?? throw new InvalidOperationException("Embedded script 20261111_OrganisationCalendar.sql was not found.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }

    // Additive feature: tables are retained on rollback so no HR calendar history is destroyed.
    protected override void Down(MigrationBuilder migrationBuilder) { }
}
