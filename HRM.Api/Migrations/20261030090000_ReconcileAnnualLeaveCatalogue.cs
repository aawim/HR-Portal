using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HRM.Migrations;

[DbContext(typeof(HrmTeContext))]
[Migration("20261030090000_ReconcileAnnualLeaveCatalogue")]
public sealed class ReconcileAnnualLeaveCatalogue : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        using var stream=typeof(ReconcileAnnualLeaveCatalogue).Assembly.GetManifestResourceStream("HRM.Database.Scripts.20261030_ReconcileAnnualLeaveCatalogue.sql")
            ?? throw new InvalidOperationException("Annual Leave reconciliation script is missing.");
        using var reader=new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }
    protected override void Down(MigrationBuilder migrationBuilder)=>throw new NotSupportedException("Restore the audited references explicitly; automatic rollback could split leave balances used after reconciliation.");
}
