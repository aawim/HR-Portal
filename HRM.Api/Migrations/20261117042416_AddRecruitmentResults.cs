using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruitmentResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EfaasIdNumber",
                table: "RM_RecruitmentApplicants",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "RM_RecruitmentResultPublications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecruitmentVacancyId = table.Column<int>(type: "int", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ObjectionClosesAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsFinal = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RM_RecruitmentResultPublications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentResultPublications_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentResultPublications_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentResultPublications_RM_RecruitmentVacancies_RecruitmentVacancyId",
                        column: x => x.RecruitmentVacancyId,
                        principalTable: "RM_RecruitmentVacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RM_RecruitmentMeritResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecruitmentResultPublicationId = table.Column<int>(type: "int", nullable: false),
                    RecruitmentApplicationId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    CalculationSnapshotJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RM_RecruitmentMeritResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentMeritResults_RM_RecruitmentApplications_RecruitmentApplicationId",
                        column: x => x.RecruitmentApplicationId,
                        principalTable: "RM_RecruitmentApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentMeritResults_RM_RecruitmentResultPublications_RecruitmentResultPublicationId",
                        column: x => x.RecruitmentResultPublicationId,
                        principalTable: "RM_RecruitmentResultPublications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RM_RecruitmentObjections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecruitmentResultPublicationId = table.Column<int>(type: "int", nullable: false),
                    RecruitmentApplicationId = table.Column<int>(type: "int", nullable: false),
                    Grounds = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RM_RecruitmentObjections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentObjections_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentObjections_RM_RecruitmentApplications_RecruitmentApplicationId",
                        column: x => x.RecruitmentApplicationId,
                        principalTable: "RM_RecruitmentApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentObjections_RM_RecruitmentResultPublications_RecruitmentResultPublicationId",
                        column: x => x.RecruitmentResultPublicationId,
                        principalTable: "RM_RecruitmentResultPublications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentApplicants_EfaasIdNumber",
                table: "RM_RecruitmentApplicants",
                column: "EfaasIdNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentMeritResults_RecruitmentApplicationId",
                table: "RM_RecruitmentMeritResults",
                column: "RecruitmentApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentMeritResults_RecruitmentResultPublicationId_RecruitmentApplicationId",
                table: "RM_RecruitmentMeritResults",
                columns: new[] { "RecruitmentResultPublicationId", "RecruitmentApplicationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentObjections_OperationLogId",
                table: "RM_RecruitmentObjections",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentObjections_RecruitmentApplicationId",
                table: "RM_RecruitmentObjections",
                column: "RecruitmentApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentObjections_RecruitmentResultPublicationId_RecruitmentApplicationId",
                table: "RM_RecruitmentObjections",
                columns: new[] { "RecruitmentResultPublicationId", "RecruitmentApplicationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentResultPublications_OperationLogId",
                table: "RM_RecruitmentResultPublications",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentResultPublications_OrganisationId",
                table: "RM_RecruitmentResultPublications",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentResultPublications_RecruitmentVacancyId",
                table: "RM_RecruitmentResultPublications",
                column: "RecruitmentVacancyId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RM_RecruitmentMeritResults");

            migrationBuilder.DropTable(
                name: "RM_RecruitmentObjections");

            migrationBuilder.DropTable(
                name: "RM_RecruitmentResultPublications");

            migrationBuilder.DropIndex(
                name: "IX_RM_RecruitmentApplicants_EfaasIdNumber",
                table: "RM_RecruitmentApplicants");

            migrationBuilder.DropColumn(
                name: "EfaasIdNumber",
                table: "RM_RecruitmentApplicants");
        }
    }
}
