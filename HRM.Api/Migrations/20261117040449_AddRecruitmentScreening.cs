using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruitmentScreening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RM_RecruitmentPanelConflictDeclarations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecruitmentVacancyId = table.Column<int>(type: "int", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    DeclaredByUserId = table.Column<int>(type: "int", nullable: false),
                    Declaration = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    HasConflict = table.Column<bool>(type: "bit", nullable: false),
                    Mitigation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DeclaredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RM_RecruitmentPanelConflictDeclarations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentPanelConflictDeclarations_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentPanelConflictDeclarations_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentPanelConflictDeclarations_RM_RecruitmentVacancies_RecruitmentVacancyId",
                        column: x => x.RecruitmentVacancyId,
                        principalTable: "RM_RecruitmentVacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RM_RecruitmentScreeningDecisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecruitmentApplicationId = table.Column<int>(type: "int", nullable: false),
                    RecruitmentVacancyId = table.Column<int>(type: "int", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    IsEligible = table.Column<bool>(type: "bit", nullable: false),
                    IsShortlisted = table.Column<bool>(type: "bit", nullable: false),
                    DecisionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    EvidenceSnapshotJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    DecidedByUserId = table.Column<int>(type: "int", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RM_RecruitmentScreeningDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentScreeningDecisions_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentScreeningDecisions_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentScreeningDecisions_RM_RecruitmentApplications_RecruitmentApplicationId",
                        column: x => x.RecruitmentApplicationId,
                        principalTable: "RM_RecruitmentApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentScreeningDecisions_RM_RecruitmentVacancies_RecruitmentVacancyId",
                        column: x => x.RecruitmentVacancyId,
                        principalTable: "RM_RecruitmentVacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentPanelConflictDeclarations_OperationLogId",
                table: "RM_RecruitmentPanelConflictDeclarations",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentPanelConflictDeclarations_OrganisationId",
                table: "RM_RecruitmentPanelConflictDeclarations",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentPanelConflictDeclarations_RecruitmentVacancyId_DeclaredByUserId",
                table: "RM_RecruitmentPanelConflictDeclarations",
                columns: new[] { "RecruitmentVacancyId", "DeclaredByUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentScreeningDecisions_OperationLogId",
                table: "RM_RecruitmentScreeningDecisions",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentScreeningDecisions_OrganisationId",
                table: "RM_RecruitmentScreeningDecisions",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentScreeningDecisions_RecruitmentApplicationId",
                table: "RM_RecruitmentScreeningDecisions",
                column: "RecruitmentApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentScreeningDecisions_RecruitmentVacancyId_IsEligible_IsShortlisted",
                table: "RM_RecruitmentScreeningDecisions",
                columns: new[] { "RecruitmentVacancyId", "IsEligible", "IsShortlisted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RM_RecruitmentPanelConflictDeclarations");

            migrationBuilder.DropTable(
                name: "RM_RecruitmentScreeningDecisions");
        }
    }
}
