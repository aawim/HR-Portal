using System;
using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations;

[DbContext(typeof(HrmTeContext))]
[Migration("20261113075000_WorkforcePolicyFoundation")]
public partial class WorkforcePolicyFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "WS_DutyTypes",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                OrganisationId = table.Column<int>(nullable: false), Code = table.Column<string>(maxLength: 64, nullable: false),
                Name = table.Column<string>(maxLength: 200, nullable: false), Category = table.Column<string>(maxLength: 50, nullable: false),
                AttendanceStrategy = table.Column<string>(maxLength: 50, nullable: false), WorkTemplateId = table.Column<int>(nullable: true), IsActive = table.Column<bool>(nullable: false),
                Version = table.Column<int>(nullable: false), OperationLogId = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WS_DutyTypes", x => x.Id);
                table.ForeignKey("FK_WS_DutyTypes_Organisations_OrganisationId", x => x.OrganisationId, "Organisations", "BusinessEntityID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_WS_DutyTypes_WorkTemplates_WorkTemplateId", x => x.WorkTemplateId, "WorkTemplates", "WorkTemplateID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_WS_DutyTypes_OperationLogs_OperationLogId", x => x.OperationLogId, "OperationLogs", "OperationLogID", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateTable(
            name: "WS_WorkforcePolicies",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"), OrganisationId = table.Column<int>(nullable: false),
                Code = table.Column<string>(maxLength: 64, nullable: false), Name = table.Column<string>(maxLength: 200, nullable: false),
                RulesJson = table.Column<string>(maxLength: 8000, nullable: false), EffectiveFrom = table.Column<DateTime>(type: "date", nullable: false),
                EffectiveTo = table.Column<DateTime>(type: "date", nullable: true), IsActive = table.Column<bool>(nullable: false),
                Version = table.Column<int>(nullable: false), OperationLogId = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WS_WorkforcePolicies", x => x.Id);
                table.CheckConstraint("CK_WS_WorkforcePolicies_EffectiveDates", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                table.ForeignKey("FK_WS_WorkforcePolicies_Organisations_OrganisationId", x => x.OrganisationId, "Organisations", "BusinessEntityID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_WS_WorkforcePolicies_OperationLogs_OperationLogId", x => x.OperationLogId, "OperationLogs", "OperationLogID", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateTable(
            name: "WS_WorkforcePolicyAssignments",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"), OrganisationId = table.Column<int>(nullable: false),
                WorkforcePolicyId = table.Column<int>(nullable: false), JobId = table.Column<int>(nullable: true), IndividualId = table.Column<int>(nullable: true),
                DutyTypeId = table.Column<int>(nullable: true), EffectiveFrom = table.Column<DateTime>(type: "date", nullable: false), EffectiveTo = table.Column<DateTime>(type: "date", nullable: true),
                Priority = table.Column<int>(nullable: false), IsActive = table.Column<bool>(nullable: false), Version = table.Column<int>(nullable: false), OperationLogId = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WS_WorkforcePolicyAssignments", x => x.Id);
                table.CheckConstraint("CK_WS_WorkforcePolicyAssignments_EffectiveDates", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                table.ForeignKey("FK_WS_WorkforcePolicyAssignments_WS_WorkforcePolicies_WorkforcePolicyId", x => x.WorkforcePolicyId, "WS_WorkforcePolicies", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_WS_WorkforcePolicyAssignments_Organisations_OrganisationId", x => x.OrganisationId, "Organisations", "BusinessEntityID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_WS_WorkforcePolicyAssignments_Jobs_JobId", x => x.JobId, "Jobs", "JobID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_WS_WorkforcePolicyAssignments_BE_Individuals_IndividualId", x => x.IndividualId, "BE_Individuals", "BusinessEntityID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_WS_WorkforcePolicyAssignments_WS_DutyTypes_DutyTypeId", x => x.DutyTypeId, "WS_DutyTypes", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_WS_WorkforcePolicyAssignments_OperationLogs_OperationLogId", x => x.OperationLogId, "OperationLogs", "OperationLogID", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("UX_WS_DutyTypes_OrganisationId_Code", "WS_DutyTypes", new[] { "OrganisationId", "Code" }, unique: true);
        migrationBuilder.CreateIndex("IX_WS_DutyTypes_WorkTemplateId", "WS_DutyTypes", "WorkTemplateId");
        migrationBuilder.CreateIndex("UX_WS_WorkforcePolicies_OrganisationId_Code_EffectiveFrom", "WS_WorkforcePolicies", new[] { "OrganisationId", "Code", "EffectiveFrom" }, unique: true);
        migrationBuilder.CreateIndex("IX_WS_WorkforcePolicyAssignments_OrganisationId_JobId_IndividualId_DutyTypeId_EffectiveFrom", "WS_WorkforcePolicyAssignments", new[] { "OrganisationId", "JobId", "IndividualId", "DutyTypeId", "EffectiveFrom" });
        migrationBuilder.CreateIndex("IX_WS_WorkforcePolicyAssignments_WorkforcePolicyId", "WS_WorkforcePolicyAssignments", "WorkforcePolicyId");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "WS_WorkforcePolicyAssignments");
        migrationBuilder.DropTable(name: "WS_DutyTypes");
        migrationBuilder.DropTable(name: "WS_WorkforcePolicies");
    }
}
