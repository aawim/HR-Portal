using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations;

/// <summary>Adds the source-verification and effective-dated CSC recruitment policy foundation.</summary>

public partial class AddRecruitmentComplianceFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "RM_RecruitmentPolicyProfiles", columns: table => new
        {
            Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            Code = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
            Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
            Jurisdiction = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            IsCscProfile = table.Column<bool>(type: "bit", nullable: false),
            IsActive = table.Column<bool>(type: "bit", nullable: false),
            Version = table.Column<int>(type: "int", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_RM_RecruitmentPolicyProfiles", x => x.Id));

        migrationBuilder.CreateTable(name: "RM_RecruitmentPolicyVersions", columns: table => new
        {
            Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            RecruitmentPolicyProfileId = table.Column<int>(type: "int", nullable: false),
            VersionCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
            OfficialSourceUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
            OfficialSourceReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
            EffectiveFrom = table.Column<DateTime>(type: "date", nullable: false),
            EffectiveTo = table.Column<DateTime>(type: "date", nullable: true),
            VerificationStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
            VerifiedByUserId = table.Column<int>(type: "int", nullable: true),
            VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
            IsApprovedForUse = table.Column<bool>(type: "bit", nullable: false),
            Version = table.Column<int>(type: "int", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_RM_RecruitmentPolicyVersions", x => x.Id);
            table.ForeignKey("FK_RM_RecruitmentPolicyVersions_RM_RecruitmentPolicyProfiles_RecruitmentPolicyProfileId", x => x.RecruitmentPolicyProfileId, "RM_RecruitmentPolicyProfiles", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_RM_RecruitmentPolicyVersions_Users_VerifiedByUserId", x => x.VerifiedByUserId, "Users", "UserID", onDelete: ReferentialAction.Restrict);
        });

        migrationBuilder.CreateTable(name: "RM_RecruitmentComplianceRules", columns: table => new
        {
            Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"), RecruitmentPolicyVersionId = table.Column<int>(type: "int", nullable: false), RuleCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false), Requirement = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false), OfficialProvision = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false), RecruitmentType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false), PositionCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false), EnforcementLevel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false), ValidationDefinitionJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false), ResponsibleAuthority = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false), VerificationStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false), IsActive = table.Column<bool>(type: "bit", nullable: false), Version = table.Column<int>(type: "int", nullable: false)
        }, constraints: table => { table.PrimaryKey("PK_RM_RecruitmentComplianceRules", x => x.Id); table.ForeignKey("FK_RM_RecruitmentComplianceRules_RM_RecruitmentPolicyVersions_RecruitmentPolicyVersionId", x => x.RecruitmentPolicyVersionId, "RM_RecruitmentPolicyVersions", "Id", onDelete: ReferentialAction.Restrict); });

        migrationBuilder.CreateTable(name: "RM_RecruitmentPolicyAssignments", columns: table => new
        {
            Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"), OrganisationId = table.Column<int>(type: "int", nullable: false), RecruitmentPolicyVersionId = table.Column<int>(type: "int", nullable: false), RecruitmentType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false), PositionCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false), EffectiveFrom = table.Column<DateTime>(type: "date", nullable: false), EffectiveTo = table.Column<DateTime>(type: "date", nullable: true), IsActive = table.Column<bool>(type: "bit", nullable: false), Version = table.Column<int>(type: "int", nullable: false), OperationLogId = table.Column<int>(type: "int", nullable: false)
        }, constraints: table => { table.PrimaryKey("PK_RM_RecruitmentPolicyAssignments", x => x.Id); table.ForeignKey("FK_RM_RecruitmentPolicyAssignments_Organisations_OrganisationId", x => x.OrganisationId, "Organisations", "BusinessEntityID", onDelete: ReferentialAction.Restrict); table.ForeignKey("FK_RM_RecruitmentPolicyAssignments_RM_RecruitmentPolicyVersions_RecruitmentPolicyVersionId", x => x.RecruitmentPolicyVersionId, "RM_RecruitmentPolicyVersions", "Id", onDelete: ReferentialAction.Restrict); table.ForeignKey("FK_RM_RecruitmentPolicyAssignments_OperationLogs_OperationLogId", x => x.OperationLogId, "OperationLogs", "OperationLogID", onDelete: ReferentialAction.Restrict); });

        migrationBuilder.CreateIndex(name: "IX_RM_RecruitmentPolicyProfiles_Code", table: "RM_RecruitmentPolicyProfiles", column: "Code", unique: true);
        migrationBuilder.CreateIndex(name: "IX_RM_RecruitmentPolicyVersions_RecruitmentPolicyProfileId_VersionCode", table: "RM_RecruitmentPolicyVersions", columns: new[] { "RecruitmentPolicyProfileId", "VersionCode" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_RM_RecruitmentPolicyVersions_VerifiedByUserId", table: "RM_RecruitmentPolicyVersions", column: "VerifiedByUserId");
        migrationBuilder.CreateIndex(name: "IX_RM_RecruitmentComplianceRules_RecruitmentPolicyVersionId_RuleCode", table: "RM_RecruitmentComplianceRules", columns: new[] { "RecruitmentPolicyVersionId", "RuleCode" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_RM_RecruitmentPolicyAssignments_OrganisationId_RecruitmentType_PositionCategory_EffectiveFrom", table: "RM_RecruitmentPolicyAssignments", columns: new[] { "OrganisationId", "RecruitmentType", "PositionCategory", "EffectiveFrom" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_RM_RecruitmentPolicyAssignments_RecruitmentPolicyVersionId", table: "RM_RecruitmentPolicyAssignments", column: "RecruitmentPolicyVersionId");
        migrationBuilder.CreateIndex(name: "IX_RM_RecruitmentPolicyAssignments_OperationLogId", table: "RM_RecruitmentPolicyAssignments", column: "OperationLogId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => throw new NotSupportedException("Preserve recruitment compliance evidence. Use a reviewed forward migration.");
}
