using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HRM.Migrations;

[DbContext(typeof(HrmTeContext))]
[Migration("20261109170000_StaffAttributeAccessControl")]
public sealed class StaffAttributeAccessControl : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AC_StaffAccessPolicies",
            columns: table => new
            {
                StaffAccessPolicyId = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                OrganisationId = table.Column<int>(nullable: false),
                PositionClassificationId = table.Column<int>(nullable: false),
                IsRestricted = table.Column<bool>(nullable: false, defaultValue: true),
                Description = table.Column<string>(maxLength: 1000, nullable: true),
                IsActive = table.Column<bool>(nullable: false, defaultValue: true),
                OperationLogId = table.Column<int>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AC_StaffAccessPolicies", x => x.StaffAccessPolicyId);
                table.ForeignKey("FK_AC_StaffAccessPolicies_Organisations", x => x.OrganisationId, "Organisations", "BusinessEntityID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_StaffAccessPolicies_PositionClassifications", x => x.PositionClassificationId, "PositionClassifications", "PositionClassificationID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_StaffAccessPolicies_OperationLogs", x => x.OperationLogId, "OperationLogs", "OperationLogID", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AC_StaffAccessScopes",
            columns: table => new
            {
                StaffAccessScopeId = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                OrganisationId = table.Column<int>(nullable: false), UserId = table.Column<int>(nullable: true),
                UserGroupId = table.Column<int>(nullable: true), RequireExplicitGrant = table.Column<bool>(nullable: false),
                IsActive = table.Column<bool>(nullable: false, defaultValue: true), OperationLogId = table.Column<int>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AC_StaffAccessScopes", x => x.StaffAccessScopeId);
                table.CheckConstraint("CK_AC_StaffAccessScopes_Subject", "([UserId] IS NOT NULL AND [UserGroupId] IS NULL) OR ([UserId] IS NULL AND [UserGroupId] IS NOT NULL)");
                table.ForeignKey("FK_AC_StaffAccessScopes_Organisations", x => x.OrganisationId, "Organisations", "BusinessEntityID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_StaffAccessScopes_Users", x => x.UserId, "Users", "UserID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_StaffAccessScopes_UserGroups", x => x.UserGroupId, "UserGroups", "UserGroupID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_StaffAccessScopes_OperationLogs", x => x.OperationLogId, "OperationLogs", "OperationLogID", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AC_StaffAccessGrants",
            columns: table => new
            {
                StaffAccessGrantId = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                OrganisationId = table.Column<int>(nullable: false), UserId = table.Column<int>(nullable: true),
                UserGroupId = table.Column<int>(nullable: true), TargetIndividualId = table.Column<int>(nullable: true),
                PositionClassificationId = table.Column<int>(nullable: true), IsAllowed = table.Column<bool>(nullable: false, defaultValue: true),
                IsActive = table.Column<bool>(nullable: false, defaultValue: true), ExpiresAtUtc = table.Column<DateTime>(nullable: true),
                Reason = table.Column<string>(maxLength: 1000, nullable: true), OperationLogId = table.Column<int>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AC_StaffAccessGrants", x => x.StaffAccessGrantId);
                table.CheckConstraint("CK_AC_StaffAccessGrants_Subject", "([UserId] IS NOT NULL AND [UserGroupId] IS NULL) OR ([UserId] IS NULL AND [UserGroupId] IS NOT NULL)");
                table.CheckConstraint("CK_AC_StaffAccessGrants_Resource", "([TargetIndividualId] IS NOT NULL AND [PositionClassificationId] IS NULL) OR ([TargetIndividualId] IS NULL AND [PositionClassificationId] IS NOT NULL)");
                table.ForeignKey("FK_AC_StaffAccessGrants_Organisations", x => x.OrganisationId, "Organisations", "BusinessEntityID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_StaffAccessGrants_Users", x => x.UserId, "Users", "UserID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_StaffAccessGrants_UserGroups", x => x.UserGroupId, "UserGroups", "UserGroupID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_StaffAccessGrants_TargetIndividuals", x => x.TargetIndividualId, "BE_Individuals", "BusinessEntityID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_StaffAccessGrants_PositionClassifications", x => x.PositionClassificationId, "PositionClassifications", "PositionClassificationID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_StaffAccessGrants_OperationLogs", x => x.OperationLogId, "OperationLogs", "OperationLogID", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("UX_AC_StaffAccessPolicies_Org_Classification", "AC_StaffAccessPolicies", new[] { "OrganisationId", "PositionClassificationId" }, unique: true);
        migrationBuilder.CreateIndex("IX_AC_StaffAccessScopes_Org_User", "AC_StaffAccessScopes", new[] { "OrganisationId", "UserId" });
        migrationBuilder.CreateIndex("IX_AC_StaffAccessScopes_Org_Group", "AC_StaffAccessScopes", new[] { "OrganisationId", "UserGroupId" });
        migrationBuilder.CreateIndex("IX_AC_StaffAccessGrants_Org_User_Active", "AC_StaffAccessGrants", new[] { "OrganisationId", "UserId", "IsActive" });
        migrationBuilder.CreateIndex("IX_AC_StaffAccessGrants_Org_Group_Active", "AC_StaffAccessGrants", new[] { "OrganisationId", "UserGroupId", "IsActive" });
        migrationBuilder.CreateIndex("IX_AC_StaffAccessGrants_Org_Target_Active", "AC_StaffAccessGrants", new[] { "OrganisationId", "TargetIndividualId", "IsActive" });

        migrationBuilder.Sql("""
            UPDATE ugr SET IsActive = 0
            FROM dbo.UserGroupRoles ugr
            INNER JOIN dbo.Roles r ON r.RoleID = ugr.RoleID
            WHERE r.RoleKey = 'Super.Admin.Manage' AND ugr.IsActive = 1;

            IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey = 'Staff.AccessPolicy.Manage')
            BEGIN
                DECLARE @ContextId int = (SELECT TOP (1) ContextId FROM dbo.Roles WHERE RoleKey = 'Admin');
                DECLARE @ModuleId int = (SELECT TOP (1) ModuleID FROM dbo.Modules WHERE Name = 'HR');
                INSERT dbo.Roles (RoleKey, Name, Description, ContextId, ModuleID, IsSystemRole)
                VALUES ('Staff.AccessPolicy.Manage', 'Manage staff access policies', 'Configure restricted staff classifications and explicit ABAC staff visibility grants.', @ContextId, @ModuleId, 1);
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Staff access policy and audit data must be preserved. Use a reviewed forward migration.");
}
