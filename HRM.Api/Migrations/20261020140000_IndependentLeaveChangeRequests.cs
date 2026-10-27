using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HRM.Migrations;

[DbContext(typeof(HrmTeContext))]
[Migration("20261020140000_IndependentLeaveChangeRequests")]
public class IndependentLeaveChangeRequests : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var assembly = typeof(IndependentLeaveChangeRequests).Assembly;
        var resource = assembly.GetManifestResourceNames().Single(x => x.EndsWith("20261020_IndependentLeaveChangeRequests.sql"));
        using var stream = assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Retain request types and number sequences that may already have staff requests.
    }
}
