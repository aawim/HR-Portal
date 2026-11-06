using System;
using System.IO;
using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    [DbContext(typeof(HrmTeContext))]
    [Migration("20261106103000_ProjectAssignments")]
    public sealed class ProjectAssignments : Migration
    {
        private static string Script()
        {
            using var stream = typeof(ProjectAssignments).Assembly.GetManifestResourceStream("HRM.Database.Scripts.20261106_ProjectAssignments.sql")
                ?? throw new InvalidOperationException("Project assignments SQL script is unavailable as an embedded resource.");
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
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS dbo.PR_ProjectAssignmentHistories;
            ");
        }
    }
}
