using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace HRM.Migrations;
public partial class PrefixBusinessEntityTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        using var stream=typeof(PrefixBusinessEntityTables).Assembly.GetManifestResourceStream("HRM.Database.Scripts.20261027_PrefixBusinessEntityTables.sql")??throw new System.InvalidOperationException("Registry rename script unavailable.");
        using var reader=new System.IO.StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }
    protected override void Down(MigrationBuilder migrationBuilder)=>throw new System.NotSupportedException("Preserve registry identities, state catalogues and constraints. Use a reviewed forward migration.");
}
