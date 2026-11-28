using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruitmentApplicants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RM_RecruitmentApplicants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reference = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Mobile = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RM_RecruitmentApplicants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RM_RecruitmentApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reference = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RecruitmentApplicantId = table.Column<int>(type: "int", nullable: false),
                    RecruitmentVacancyId = table.Column<int>(type: "int", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ProfileSnapshotJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RM_RecruitmentApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentApplications_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentApplications_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentApplications_RM_RecruitmentApplicants_RecruitmentApplicantId",
                        column: x => x.RecruitmentApplicantId,
                        principalTable: "RM_RecruitmentApplicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentApplications_RM_RecruitmentVacancies_RecruitmentVacancyId",
                        column: x => x.RecruitmentVacancyId,
                        principalTable: "RM_RecruitmentVacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentApplicants_Email",
                table: "RM_RecruitmentApplicants",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentApplicants_Reference",
                table: "RM_RecruitmentApplicants",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentApplications_OperationLogId",
                table: "RM_RecruitmentApplications",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentApplications_OrganisationId_Status_SubmittedAtUtc",
                table: "RM_RecruitmentApplications",
                columns: new[] { "OrganisationId", "Status", "SubmittedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentApplications_RecruitmentApplicantId_RecruitmentVacancyId",
                table: "RM_RecruitmentApplications",
                columns: new[] { "RecruitmentApplicantId", "RecruitmentVacancyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentApplications_RecruitmentVacancyId",
                table: "RM_RecruitmentApplications",
                column: "RecruitmentVacancyId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentApplications_Reference",
                table: "RM_RecruitmentApplications",
                column: "Reference",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RM_RecruitmentApplications");

            migrationBuilder.DropTable(
                name: "RM_RecruitmentApplicants");
        }
    }
}
