using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruitmentOperationsFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RM_RecruitmentRequisitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reference = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    OrganisationStructureId = table.Column<int>(type: "int", nullable: true),
                    PositionMatrixId = table.Column<int>(type: "int", nullable: false),
                    RecruitmentPolicyVersionId = table.Column<int>(type: "int", nullable: false),
                    RequestedSlots = table.Column<int>(type: "int", nullable: false),
                    EmploymentType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    RecruitmentType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PositionSnapshotJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RM_RecruitmentRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentRequisitions_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentRequisitions_OrganisationStructure_OrganisationStructureId",
                        column: x => x.OrganisationStructureId,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentRequisitions_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentRequisitions_PF_PositionMatrix_PositionMatrixId",
                        column: x => x.PositionMatrixId,
                        principalTable: "PF_PositionMatrix",
                        principalColumn: "PositionMatrixID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentRequisitions_RM_RecruitmentPolicyVersions_RecruitmentPolicyVersionId",
                        column: x => x.RecruitmentPolicyVersionId,
                        principalTable: "RM_RecruitmentPolicyVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentRequisitions_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RM_RecruitmentPositionReservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    PositionMatrixId = table.Column<int>(type: "int", nullable: false),
                    RecruitmentRequisitionId = table.Column<int>(type: "int", nullable: false),
                    ReservedSlots = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReservedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReleasedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RM_RecruitmentPositionReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentPositionReservations_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentPositionReservations_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentPositionReservations_PF_PositionMatrix_PositionMatrixId",
                        column: x => x.PositionMatrixId,
                        principalTable: "PF_PositionMatrix",
                        principalColumn: "PositionMatrixID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentPositionReservations_RM_RecruitmentRequisitions_RecruitmentRequisitionId",
                        column: x => x.RecruitmentRequisitionId,
                        principalTable: "RM_RecruitmentRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RM_RecruitmentVacancies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reference = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    RecruitmentRequisitionId = table.Column<int>(type: "int", nullable: false),
                    RecruitmentPolicyVersionId = table.Column<int>(type: "int", nullable: false),
                    PositionMatrixId = table.Column<int>(type: "int", nullable: false),
                    VacancySlots = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AdvertisementSnapshotJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosingAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RM_RecruitmentVacancies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentVacancies_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentVacancies_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentVacancies_PF_PositionMatrix_PositionMatrixId",
                        column: x => x.PositionMatrixId,
                        principalTable: "PF_PositionMatrix",
                        principalColumn: "PositionMatrixID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentVacancies_RM_RecruitmentPolicyVersions_RecruitmentPolicyVersionId",
                        column: x => x.RecruitmentPolicyVersionId,
                        principalTable: "RM_RecruitmentPolicyVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentVacancies_RM_RecruitmentRequisitions_RecruitmentRequisitionId",
                        column: x => x.RecruitmentRequisitionId,
                        principalTable: "RM_RecruitmentRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentPositionReservations_OperationLogId",
                table: "RM_RecruitmentPositionReservations",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentPositionReservations_OrganisationId",
                table: "RM_RecruitmentPositionReservations",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentPositionReservations_PositionMatrixId_Status",
                table: "RM_RecruitmentPositionReservations",
                columns: new[] { "PositionMatrixId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentPositionReservations_RecruitmentRequisitionId_Status",
                table: "RM_RecruitmentPositionReservations",
                columns: new[] { "RecruitmentRequisitionId", "Status" },
                unique: true,
                filter: "[Status] = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentRequisitions_OperationLogId",
                table: "RM_RecruitmentRequisitions",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentRequisitions_OrganisationId_Status_CreatedAtUtc",
                table: "RM_RecruitmentRequisitions",
                columns: new[] { "OrganisationId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentRequisitions_OrganisationStructureId",
                table: "RM_RecruitmentRequisitions",
                column: "OrganisationStructureId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentRequisitions_PositionMatrixId",
                table: "RM_RecruitmentRequisitions",
                column: "PositionMatrixId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentRequisitions_RecruitmentPolicyVersionId",
                table: "RM_RecruitmentRequisitions",
                column: "RecruitmentPolicyVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentRequisitions_Reference",
                table: "RM_RecruitmentRequisitions",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentRequisitions_RequestedByUserId",
                table: "RM_RecruitmentRequisitions",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentVacancies_OperationLogId",
                table: "RM_RecruitmentVacancies",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentVacancies_OrganisationId_Status_ClosingAtUtc",
                table: "RM_RecruitmentVacancies",
                columns: new[] { "OrganisationId", "Status", "ClosingAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentVacancies_PositionMatrixId",
                table: "RM_RecruitmentVacancies",
                column: "PositionMatrixId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentVacancies_RecruitmentPolicyVersionId",
                table: "RM_RecruitmentVacancies",
                column: "RecruitmentPolicyVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentVacancies_RecruitmentRequisitionId",
                table: "RM_RecruitmentVacancies",
                column: "RecruitmentRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentVacancies_Reference",
                table: "RM_RecruitmentVacancies",
                column: "Reference",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RM_RecruitmentPositionReservations");

            migrationBuilder.DropTable(
                name: "RM_RecruitmentVacancies");

            migrationBuilder.DropTable(
                name: "RM_RecruitmentRequisitions");
        }
    }
}
