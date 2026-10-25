using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class TemplateDerivedWorkingHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            using var stream = typeof(TemplateDerivedWorkingHours).Assembly.GetManifestResourceStream("HRM.Database.Scripts.20261025_TemplateDerivedWorkingHours.sql")
                ?? throw new InvalidOperationException("Temporary working-hours installation script is unavailable.");
            using var reader = new System.IO.StreamReader(stream);
            migrationBuilder.Sql(reader.ReadToEnd());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Preserve working-hours history; roll back application code without dropping configuration tables.");
        }
    }
}
