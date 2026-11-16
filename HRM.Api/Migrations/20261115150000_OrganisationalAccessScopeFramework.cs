using System;
using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HRM.Migrations;

[DbContext(typeof(HrmTeContext))]
[Migration("20261115150000_OrganisationalAccessScopeFramework")]
public sealed class OrganisationalAccessScopeFramework : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AC_UserAccessScopes",
            columns: table => new
            {
                UserAccessScopeId = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                OrganisationId = table.Column<int>(nullable: false),
                UserId = table.Column<int>(nullable: true),
                UserGroupId = table.Column<int>(nullable: true),
                UnitId = table.Column<int>(nullable: true),
                TeamId = table.Column<int>(nullable: true),
                ScopeType = table.Column<string>(maxLength: 50, nullable: false),
                IncludeDescendants = table.Column<bool>(nullable: false, defaultValue: true),
                PermissionFilter = table.Column<string>(maxLength: 200, nullable: true),
                ValidFrom = table.Column<DateTime>(nullable: false),
                ValidTo = table.Column<DateTime>(nullable: true),
                IsActive = table.Column<bool>(nullable: false, defaultValue: true),
                GrantedByUserId = table.Column<int>(nullable: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                RevokedByUserId = table.Column<int>(nullable: true),
                RevokedAtUtc = table.Column<DateTime>(nullable: true),
                RevocationReason = table.Column<string>(maxLength: 1000, nullable: true),
                Reason = table.Column<string>(maxLength: 1000, nullable: true),
                OperationLogId = table.Column<int>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AC_UserAccessScopes", x => x.UserAccessScopeId);
                table.CheckConstraint("CK_AC_UserAccessScopes_Subject",
                    "([UserId] IS NOT NULL AND [UserGroupId] IS NULL) OR ([UserId] IS NULL AND [UserGroupId] IS NOT NULL)");
                table.CheckConstraint("CK_AC_UserAccessScopes_ScopeType",
                    "[ScopeType] IN ('Self', 'Team', 'Department', 'Division', 'DivisionAndDescendants', 'Organisation', 'Custom')");
                table.ForeignKey("FK_AC_UserAccessScopes_Organisations", x => x.OrganisationId, "Organisations", "BusinessEntityID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_UserAccessScopes_Users", x => x.UserId, "Users", "UserID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_UserAccessScopes_UserGroups", x => x.UserGroupId, "UserGroups", "UserGroupID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_UserAccessScopes_Units", x => x.UnitId, "OrganisationStructures", "OrganisationStructureID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_UserAccessScopes_Teams", x => x.TeamId, "Teams", "TeamID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_UserAccessScopes_GrantedByUsers", x => x.GrantedByUserId, "Users", "UserID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_UserAccessScopes_RevokedByUsers", x => x.RevokedByUserId, "Users", "UserID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_UserAccessScopes_OperationLogs", x => x.OperationLogId, "OperationLogs", "OperationLogID", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AC_UserAccessScopeAudits",
            columns: table => new
            {
                UserAccessScopeAuditId = table.Column<long>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                UserAccessScopeId = table.Column<int>(nullable: true),
                OrganisationId = table.Column<int>(nullable: false),
                TargetUserId = table.Column<int>(nullable: true),
                TargetUserGroupId = table.Column<int>(nullable: true),
                Action = table.Column<string>(maxLength: 100, nullable: false),
                ScopeType = table.Column<string>(maxLength: 50, nullable: false),
                UnitId = table.Column<int>(nullable: true),
                TeamId = table.Column<int>(nullable: true),
                IncludeDescendants = table.Column<bool>(nullable: false),
                ValidFrom = table.Column<DateTime>(nullable: true),
                ValidTo = table.Column<DateTime>(nullable: true),
                PerformedByUserId = table.Column<int>(nullable: false),
                TimestampUtc = table.Column<DateTime>(nullable: false),
                Detail = table.Column<string>(maxLength: 2000, nullable: true),
                OperationLogId = table.Column<int>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AC_UserAccessScopeAudits", x => x.UserAccessScopeAuditId);
                table.ForeignKey("FK_AC_UserAccessScopeAudits_Scopes", x => x.UserAccessScopeId, "AC_UserAccessScopes", "UserAccessScopeId", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_UserAccessScopeAudits_Organisations", x => x.OrganisationId, "Organisations", "BusinessEntityID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_UserAccessScopeAudits_TargetUsers", x => x.TargetUserId, "Users", "UserID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_UserAccessScopeAudits_TargetUserGroups", x => x.TargetUserGroupId, "UserGroups", "UserGroupID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_UserAccessScopeAudits_Units", x => x.UnitId, "OrganisationStructures", "OrganisationStructureID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_UserAccessScopeAudits_Teams", x => x.TeamId, "Teams", "TeamID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_UserAccessScopeAudits_PerformedByUsers", x => x.PerformedByUserId, "Users", "UserID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AC_UserAccessScopeAudits_OperationLogs", x => x.OperationLogId, "OperationLogs", "OperationLogID", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_AC_UserAccessScopes_Org_User_Active", "AC_UserAccessScopes", new[] { "OrganisationId", "UserId", "IsActive" });
        migrationBuilder.CreateIndex("IX_AC_UserAccessScopes_Org_Group_Active", "AC_UserAccessScopes", new[] { "OrganisationId", "UserGroupId", "IsActive" });
        migrationBuilder.CreateIndex("IX_AC_UserAccessScopes_Org_Unit_Active", "AC_UserAccessScopes", new[] { "OrganisationId", "UnitId", "IsActive" });
        migrationBuilder.CreateIndex("IX_AC_UserAccessScopeAudits_Org_Time", "AC_UserAccessScopeAudits", new[] { "OrganisationId", "TimestampUtc" });
        migrationBuilder.CreateIndex("IX_AC_UserAccessScopeAudits_TargetUser_Time", "AC_UserAccessScopeAudits", new[] { "TargetUserId", "TimestampUtc" });

        migrationBuilder.Sql("""
            DECLARE @ContextId int = ISNULL((SELECT TOP (1) ContextId FROM dbo.Roles WHERE RoleKey = 'Admin'), 1);
            DECLARE @ModuleId int = (SELECT TOP (1) ModuleID FROM dbo.Modules WHERE Name = 'HR');

            IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey = 'Staff.AccessScope.Manage')
                INSERT dbo.Roles (RoleKey, Name, Description, ContextId, ModuleID, IsSystemRole)
                VALUES ('Staff.AccessScope.Manage', 'Manage organisational access scopes', 'Configure division, department, team, and custom hierarchical access scopes.', @ContextId, @ModuleId, 1);

            IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey = 'Staff.AccessScope.Simulate')
                INSERT dbo.Roles (RoleKey, Name, Description, ContextId, ModuleID, IsSystemRole)
                VALUES ('Staff.AccessScope.Simulate', 'Simulate user effective access', 'Preview server-calculated effective organisational scopes and field permissions.', @ContextId, @ModuleId, 1);

            IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey = 'Employee.PrivateRecord.View')
                INSERT dbo.Roles (RoleKey, Name, Description, ContextId, ModuleID, IsSystemRole)
                VALUES ('Employee.PrivateRecord.View', 'View private employee HR records', 'View confidential employee HR records within authorised organisational scopes.', @ContextId, @ModuleId, 1);

            IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey = 'Employee.Salary.View')
                INSERT dbo.Roles (RoleKey, Name, Description, ContextId, ModuleID, IsSystemRole)
                VALUES ('Employee.Salary.View', 'View employee salary and bank details', 'View employee compensation, salary, and bank account details within authorised scopes.', @ContextId, @ModuleId, 1);

            IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey = 'Employee.Identification.View')
                INSERT dbo.Roles (RoleKey, Name, Description, ContextId, ModuleID, IsSystemRole)
                VALUES ('Employee.Identification.View', 'View employee identification details', 'View national ID card, passport, and emergency contact details within authorised scopes.', @ContextId, @ModuleId, 1);

            IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey = 'Employee.Disciplinary.View')
                INSERT dbo.Roles (RoleKey, Name, Description, ContextId, ModuleID, IsSystemRole)
                VALUES ('Employee.Disciplinary.View', 'View employee disciplinary records', 'View confidential disciplinary records within authorised scopes.', @ContextId, @ModuleId, 1);

            IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey = 'Employee.Medical.View')
                INSERT dbo.Roles (RoleKey, Name, Description, ContextId, ModuleID, IsSystemRole)
                VALUES ('Employee.Medical.View', 'View employee medical information', 'View confidential medical and health records within authorised scopes.', @ContextId, @ModuleId, 1);

            IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey = 'Directory.PublicProfile.View')
                INSERT dbo.Roles (RoleKey, Name, Description, ContextId, ModuleID, IsSystemRole)
                VALUES ('Directory.PublicProfile.View', 'View public directory profiles', 'View basic public staff directory profile information.', @ContextId, @ModuleId, 1);

            IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey = 'Intranet.Announcement.View')
                INSERT dbo.Roles (RoleKey, Name, Description, ContextId, ModuleID, IsSystemRole)
                VALUES ('Intranet.Announcement.View', 'View intranet announcements', 'View public or audience-targeted intranet announcements without exposing private HR records.', @ContextId, @ModuleId, 1);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Organisational access scope and audit data must be preserved. Use a reviewed forward migration.");
}
