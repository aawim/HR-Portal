using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class ScopeScheduleAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            using var stream = typeof(ScopeScheduleAssignments).Assembly.GetManifestResourceStream("HRM.Database.Scripts.20261025_ScopeScheduleAssignments.sql")
                ?? throw new InvalidOperationException("Scope schedule installation script is unavailable.");
            using var reader = new System.IO.StreamReader(stream);
            migrationBuilder.Sql(reader.ReadToEnd());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Preserve schedule assignment history; roll back application code without dropping this table.");
        }
    }
}
