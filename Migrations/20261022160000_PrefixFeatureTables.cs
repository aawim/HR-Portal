using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HRM.Migrations;

[DbContext(typeof(HrmTeContext))]
[Migration("20261022160000_PrefixFeatureTables")]
public sealed class PrefixFeatureTables : Migration
{
    private static string Script()
    {
        using var stream = typeof(PrefixFeatureTables).Assembly.GetManifestResourceStream("HRM.Database.Scripts.20261022_PrefixFeatureTables.sql")
            ?? throw new InvalidOperationException("Feature-table rename script is unavailable.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(Script());
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(Script().Replace("DECLARE @Revert bit = 0;", "DECLARE @Revert bit = 1;"));
}
