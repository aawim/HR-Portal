using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruitmentOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RM_RecruitmentOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecruitmentMeritResultId = table.Column<int>(type: "int", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    AppointmentAuthorityUserId = table.Column<int>(type: "int", nullable: false),
                    TermsSnapshotJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OfferedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RM_RecruitmentOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentOffers_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentOffers_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentOffers_RM_RecruitmentMeritResults_RecruitmentMeritResultId",
                        column: x => x.RecruitmentMeritResultId,
                        principalTable: "RM_RecruitmentMeritResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentOffers_OperationLogId",
                table: "RM_RecruitmentOffers",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentOffers_OrganisationId",
                table: "RM_RecruitmentOffers",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentOffers_RecruitmentMeritResultId",
                table: "RM_RecruitmentOffers",
                column: "RecruitmentMeritResultId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RM_RecruitmentOffers");
        }
    }
}
