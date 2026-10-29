using Microsoft.EntityFrameworkCore.Migrations;
namespace HRM.Migrations;

public partial class RetireUnusedLegacyTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        using var stream=typeof(RetireUnusedLegacyTables).Assembly.GetManifestResourceStream("HRM.Database.Scripts.20261029_RetireUnusedLegacyTables.sql")
            ??throw new InvalidOperationException("Reviewed legacy table cleanup script is unavailable.");
        using var reader=new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }
    protected override void Down(MigrationBuilder migrationBuilder)=>throw new NotSupportedException("Preserve retirement evidence and retained table identities. Use a reviewed recovery/forward migration.");
}
