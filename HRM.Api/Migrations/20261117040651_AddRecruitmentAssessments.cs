using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruitmentAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RM_RecruitmentAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecruitmentVacancyId = table.Column<int>(type: "int", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TemplateCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RM_RecruitmentAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentAssessments_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentAssessments_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentAssessments_RM_RecruitmentVacancies_RecruitmentVacancyId",
                        column: x => x.RecruitmentVacancyId,
                        principalTable: "RM_RecruitmentVacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RM_RecruitmentPanelScorecards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecruitmentAssessmentId = table.Column<int>(type: "int", nullable: false),
                    RecruitmentApplicationId = table.Column<int>(type: "int", nullable: false),
                    ScorerUserId = table.Column<int>(type: "int", nullable: false),
                    TotalScore = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    CriterionScoresJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RM_RecruitmentPanelScorecards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentPanelScorecards_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentPanelScorecards_RM_RecruitmentApplications_RecruitmentApplicationId",
                        column: x => x.RecruitmentApplicationId,
                        principalTable: "RM_RecruitmentApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentPanelScorecards_RM_RecruitmentAssessments_RecruitmentAssessmentId",
                        column: x => x.RecruitmentAssessmentId,
                        principalTable: "RM_RecruitmentAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentAssessments_OperationLogId",
                table: "RM_RecruitmentAssessments",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentAssessments_OrganisationId",
                table: "RM_RecruitmentAssessments",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentAssessments_RecruitmentVacancyId_ScheduledAtUtc",
                table: "RM_RecruitmentAssessments",
                columns: new[] { "RecruitmentVacancyId", "ScheduledAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentPanelScorecards_OperationLogId",
                table: "RM_RecruitmentPanelScorecards",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentPanelScorecards_RecruitmentApplicationId",
                table: "RM_RecruitmentPanelScorecards",
                column: "RecruitmentApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentPanelScorecards_RecruitmentAssessmentId_RecruitmentApplicationId_ScorerUserId",
                table: "RM_RecruitmentPanelScorecards",
                columns: new[] { "RecruitmentAssessmentId", "RecruitmentApplicationId", "ScorerUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RM_RecruitmentPanelScorecards");

            migrationBuilder.DropTable(
                name: "RM_RecruitmentAssessments");
        }
    }
}
