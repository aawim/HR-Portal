using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class JobTypeLeaveSetAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LS_JobTypeLeaveSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    JobTypeId = table.Column<int>(type: "int", nullable: false),
                    LeaveSetId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LS_JobTypeLeaveSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LS_JobTypeLeaveSets_JobTypes_JobTypeId",
                        column: x => x.JobTypeId,
                        principalTable: "JobTypes",
                        principalColumn: "JobTypeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LS_JobTypeLeaveSets_LeaveSets_LeaveSetId",
                        column: x => x.LeaveSetId,
                        principalTable: "LeaveSets",
                        principalColumn: "LeaveSetID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LS_JobTypeLeaveSets_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LS_JobTypeLeaveSets_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LS_JobTypeLeaveSets_JobTypeId",
                table: "LS_JobTypeLeaveSets",
                column: "JobTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LS_JobTypeLeaveSets_LeaveSetId",
                table: "LS_JobTypeLeaveSets",
                column: "LeaveSetId");

            migrationBuilder.CreateIndex(
                name: "IX_LS_JobTypeLeaveSets_OperationLogId",
                table: "LS_JobTypeLeaveSets",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_LS_JobTypeLeaveSets_OrganisationId_JobTypeId_LeaveSetId",
                table: "LS_JobTypeLeaveSets",
                columns: new[] { "OrganisationId", "JobTypeId", "LeaveSetId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
            => throw new System.NotSupportedException("Preserve Job Type leave set assignments and audit history; use a reviewed forward migration.");
    }
}
