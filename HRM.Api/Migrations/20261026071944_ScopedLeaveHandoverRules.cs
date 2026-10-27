using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class ScopedLeaveHandoverRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HandoverRuleSnapshot",
                table: "Leaves",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LH_LeaveHandoverRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OrganisationStructureId = table.Column<int>(type: "int", nullable: true),
                    TeamId = table.Column<int>(type: "int", nullable: true),
                    JobTypeId = table.Column<int>(type: "int", nullable: true),
                    AllLeaveTypes = table.Column<bool>(type: "bit", nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AfterDays = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LH_LeaveHandoverRules", x => x.Id);
                    table.CheckConstraint("CK_LH_RuleScope", "([Scope]='Organisation' AND [TeamId] IS NULL AND [OrganisationStructureId] IS NULL) OR ([Scope]='Structure' AND [OrganisationStructureId] IS NOT NULL AND [TeamId] IS NULL) OR ([Scope]='Team' AND [TeamId] IS NOT NULL AND [OrganisationStructureId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_LH_LeaveHandoverRules_JobTypes_JobTypeId",
                        column: x => x.JobTypeId,
                        principalTable: "JobTypes",
                        principalColumn: "JobTypeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LH_LeaveHandoverRules_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LH_LeaveHandoverRules_OrganisationStructure_OrganisationStructureId",
                        column: x => x.OrganisationStructureId,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LH_LeaveHandoverRules_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LH_LeaveHandoverRules_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "TeamID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LH_LeaveHandoverRuleTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleId = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LH_LeaveHandoverRuleTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LH_LeaveHandoverRuleTypes_LH_LeaveHandoverRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "LH_LeaveHandoverRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LH_LeaveHandoverRuleTypes_LT_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LT_LeaveTypes",
                        principalColumn: "LeaveTypeID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LH_LeaveHandoverRules_JobTypeId",
                table: "LH_LeaveHandoverRules",
                column: "JobTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LH_LeaveHandoverRules_OperationLogId",
                table: "LH_LeaveHandoverRules",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_LH_LeaveHandoverRules_OrganisationId_IsActive_EffectiveFrom",
                table: "LH_LeaveHandoverRules",
                columns: new[] { "OrganisationId", "IsActive", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_LH_LeaveHandoverRules_OrganisationStructureId",
                table: "LH_LeaveHandoverRules",
                column: "OrganisationStructureId");

            migrationBuilder.CreateIndex(
                name: "IX_LH_LeaveHandoverRules_TeamId",
                table: "LH_LeaveHandoverRules",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_LH_LeaveHandoverRuleTypes_LeaveTypeId",
                table: "LH_LeaveHandoverRuleTypes",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LH_LeaveHandoverRuleTypes_RuleId_LeaveTypeId",
                table: "LH_LeaveHandoverRuleTypes",
                columns: new[] { "RuleId", "LeaveTypeId" },
                unique: true);
            using var stream = typeof(ScopedLeaveHandoverRules).Assembly.GetManifestResourceStream("HRM.Database.Scripts.20261026_ScopedLeaveHandoverRules.sql")
                ?? throw new InvalidOperationException("Scoped handover metadata script is missing.");
            using var reader = new StreamReader(stream);
            migrationBuilder.Sql(reader.ReadToEnd());

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
            => throw new NotSupportedException("Scoped handover rules and submitted policy snapshots must be preserved. Use a reviewed data-preserving rollback.");
    }
}
