using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class JobTypeOvertimeRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OT_JobTypeOvertimeRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    JobTypeId = table.Column<int>(type: "int", nullable: false),
                    IsAllowed = table.Column<bool>(type: "bit", nullable: false),
                    MaximumWeeklyHours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MaximumDailyHours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OT_JobTypeOvertimeRules", x => x.Id);
                    table.CheckConstraint("CK_OT_JobTypeOvertimeRules_Limits", "([IsAllowed] = 0 AND [MaximumWeeklyHours] = 0 AND [MaximumDailyHours] IS NULL) OR ([IsAllowed] = 1 AND [MaximumWeeklyHours] > 0 AND [MaximumWeeklyHours] <= 168 AND ([MaximumDailyHours] IS NULL OR ([MaximumDailyHours] > 0 AND [MaximumDailyHours] <= 24 AND [MaximumDailyHours] <= [MaximumWeeklyHours])))");
                    table.ForeignKey(
                        name: "FK_OT_JobTypeOvertimeRules_JobTypes_JobTypeId",
                        column: x => x.JobTypeId,
                        principalTable: "JobTypes",
                        principalColumn: "JobTypeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OT_JobTypeOvertimeRules_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OT_JobTypeOvertimeRules_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OT_JobTypeOvertimeRules_JobTypeId",
                table: "OT_JobTypeOvertimeRules",
                column: "JobTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OT_JobTypeOvertimeRules_OperationLogId",
                table: "OT_JobTypeOvertimeRules",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_OT_JobTypeOvertimeRules_OrganisationId_JobTypeId",
                table: "OT_JobTypeOvertimeRules",
                columns: new[] { "OrganisationId", "JobTypeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new System.NotSupportedException("Preserve overtime configuration history; roll back application code without dropping this table.");
        }
    }
}
