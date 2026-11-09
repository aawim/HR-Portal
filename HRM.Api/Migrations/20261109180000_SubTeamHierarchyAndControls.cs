using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HRM.Migrations;

[DbContext(typeof(HrmTeContext))]
[Migration("20261109180000_SubTeamHierarchyAndControls")]
public sealed class SubTeamHierarchyAndControls : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "ParentTeamID", table: "Teams", nullable: true);
        migrationBuilder.AddColumn<int>(name: "OrganisationStructureID", table: "Teams", nullable: true);
        migrationBuilder.AddColumn<bool>(name: "OvertimeEnabled", table: "Teams", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<decimal>(name: "MaximumDailyOvertimeHours", table: "Teams", type: "decimal(5,2)", nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "MaximumWeeklyOvertimeHours", table: "Teams", type: "decimal(5,2)", nullable: true);
        migrationBuilder.AddColumn<string>(name: "MemberRole", table: "TeamStaffs", maxLength: 20, nullable: false, defaultValue: "Member");
        migrationBuilder.Sql("UPDATE TeamStaffs SET MemberRole = CASE WHEN IsSuperVisor = 1 THEN 'Supervisor' ELSE 'Member' END;");
        migrationBuilder.CreateIndex(name: "IX_Teams_ParentTeamID", table: "Teams", column: "ParentTeamID");
        migrationBuilder.CreateIndex(name: "IX_Teams_OrganisationStructureID", table: "Teams", column: "OrganisationStructureID");
        migrationBuilder.AddForeignKey(name: "FK_Teams_ParentTeam", table: "Teams", column: "ParentTeamID", principalTable: "Teams", principalColumn: "TeamID", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_Teams_OrganisationStructures", table: "Teams", column: "OrganisationStructureID", principalTable: "OrganisationStructure", principalColumn: "OrganisationStructureID", onDelete: ReferentialAction.Restrict);
        migrationBuilder.CreateTable(name: "TM_TeamScheduleAdjustments", columns: table => new
        {
            Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            TeamId = table.Column<int>(nullable: false), WorkDate = table.Column<DateTime>(type: "date", nullable: false),
            RevisedCheckIn = table.Column<TimeSpan>(type: "time", nullable: true), RevisedCheckOut = table.Column<TimeSpan>(type: "time", nullable: true),
            Reason = table.Column<string>(maxLength: 1000, nullable: false), IsActive = table.Column<bool>(nullable: false, defaultValue: true),
            CreatedByUserId = table.Column<int>(nullable: false), CreatedAtUtc = table.Column<DateTime>(nullable: false),
            Version = table.Column<int>(nullable: false, defaultValue: 1), OperationLogId = table.Column<int>(nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_TM_TeamScheduleAdjustments", x => x.Id);
            table.CheckConstraint("CK_TM_TeamScheduleAdjustments_Time", "[RevisedCheckIn] IS NOT NULL OR [RevisedCheckOut] IS NOT NULL");
            table.ForeignKey("FK_TM_TeamScheduleAdjustments_Teams", x => x.TeamId, "Teams", "TeamID", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_TM_TeamScheduleAdjustments_Users", x => x.CreatedByUserId, "Users", "UserID", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_TM_TeamScheduleAdjustments_OperationLogs", x => x.OperationLogId, "OperationLogs", "OperationLogID", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex(name: "IX_TM_TeamScheduleAdjustments_TeamId_WorkDate_IsActive", table: "TM_TeamScheduleAdjustments", columns: new[] { "TeamId", "WorkDate", "IsActive" });
        migrationBuilder.CreateIndex(name: "IX_TM_TeamScheduleAdjustments_CreatedByUserId", table: "TM_TeamScheduleAdjustments", column: "CreatedByUserId");
        migrationBuilder.CreateIndex(name: "IX_TM_TeamScheduleAdjustments_OperationLogId", table: "TM_TeamScheduleAdjustments", column: "OperationLogId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new InvalidOperationException("This migration contains audited team schedule and hierarchy data and cannot be automatically reversed.");
}
