using System;
using System.IO;
using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    [DbContext(typeof(HrmTeContext))]
    [Migration("20261106093000_WorkspaceTeamsProjectsEvaluations")]
    public sealed class WorkspaceTeamsProjectsEvaluations : Migration
    {
        private static string Script()
        {
            using var stream = typeof(WorkspaceTeamsProjectsEvaluations).Assembly.GetManifestResourceStream("HRM.Database.Scripts.20261106_WorkspaceTeamsProjectsEvaluations.sql")
                ?? throw new InvalidOperationException("Workspace teams, projects and evaluations SQL script is unavailable as an embedded resource.");
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
                DROP TABLE IF EXISTS dbo.EV_EvaluationScores;
                DROP TABLE IF EXISTS dbo.EV_Evaluations;
                DROP TABLE IF EXISTS dbo.PR_ProjectTasks;
                DROP TABLE IF EXISTS dbo.PR_ProjectMembers;
                DROP TABLE IF EXISTS dbo.PR_Projects;
            ");
        }
    }
}
