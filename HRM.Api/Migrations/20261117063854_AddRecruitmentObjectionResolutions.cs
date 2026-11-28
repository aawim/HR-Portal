using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruitmentObjectionResolutions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Resolution",
                table: "RM_RecruitmentObjections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAtUtc",
                table: "RM_RecruitmentObjections",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResolvedByUserId",
                table: "RM_RecruitmentObjections",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Resolution",
                table: "RM_RecruitmentObjections");

            migrationBuilder.DropColumn(
                name: "ResolvedAtUtc",
                table: "RM_RecruitmentObjections");

            migrationBuilder.DropColumn(
                name: "ResolvedByUserId",
                table: "RM_RecruitmentObjections");
        }
    }
}
