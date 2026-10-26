using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class PrefixLeavePolicyTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            using var stream = typeof(PrefixLeavePolicyTables).Assembly.GetManifestResourceStream("HRM.Database.Scripts.20261026_PrefixLeavePolicyTables.sql")
                ?? throw new InvalidOperationException("Leave policy table naming script is missing.");
            using var reader = new StreamReader(stream);
            migrationBuilder.Sql(reader.ReadToEnd());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "LT_LeavePolicyAccrualRules",
                newName: "LeavePolicyAccrualRules");

            migrationBuilder.RenameTable(
                name: "LT_LeavePolicies",
                newName: "LeavePolicies");
        }
    }
}
