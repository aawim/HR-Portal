using System;
using System.IO;
using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    [DbContext(typeof(HrmTeContext))]
    [Migration("20261103140000_AttendanceReconciliationEngine")]
    public sealed class AttendanceReconciliationEngine : Migration
    {
        private static string Script()
        {
            using var stream = typeof(AttendanceReconciliationEngine).Assembly.GetManifestResourceStream("HRM.Database.Scripts.20261103_AttendanceReconciliationEngine.sql")
                ?? throw new InvalidOperationException("Attendance reconciliation engine SQL script is unavailable as an embedded resource.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Script());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Attendance reconciliation records and audit history must be retained.");
        }
    }
}
