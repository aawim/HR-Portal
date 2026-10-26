using Microsoft.EntityFrameworkCore.Migrations;
using System.Text.RegularExpressions;
#nullable disable
namespace HRM.Migrations;

public partial class UnifiedLeaveTypes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        using var stream = typeof(UnifiedLeaveTypes).Assembly.GetManifestResourceStream("HRM.Database.Scripts.20261026_UnifiedLeaveTypes.sql")
            ?? throw new InvalidOperationException("Leave type consolidation script is missing.");
        using var reader = new StreamReader(stream);
        foreach (var batch in Regex.Split(reader.ReadToEnd(), @"^GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            if (!string.IsNullOrWhiteSpace(batch)) migrationBuilder.Sql(batch);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
        => throw new NotSupportedException("Leave type consolidation preserves historical identities and cannot be undone automatically. Restore the verified pre-upgrade backup to roll back.");
}
