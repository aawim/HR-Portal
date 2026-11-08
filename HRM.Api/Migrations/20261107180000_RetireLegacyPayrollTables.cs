using System;
using System.IO;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HRM.Migrations;

[DbContext(typeof(HRM.Models.HrmTeContext))]
[Migration("20261107180000_RetireLegacyPayrollTables")]
public partial class RetireLegacyPayrollTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        using var stream = typeof(RetireLegacyPayrollTables).Assembly.GetManifestResourceStream("HRM.Database.Scripts.20261107_RetireLegacyPayrollTables.sql")
            ?? throw new InvalidOperationException("Reviewed legacy payroll table cleanup script is unavailable.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Preserve retirement evidence. Use a reviewed recovery/forward migration.");
    }
}
