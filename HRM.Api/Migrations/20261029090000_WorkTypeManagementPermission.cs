using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace HRM.Migrations;

[DbContext(typeof(HrmTeContext))]
[Migration("20261029090000_WorkTypeManagementPermission")]
public class WorkTypeManagementPermission : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleKey = 'WorkTypes.Manage')
        BEGIN
            DECLARE @ContextId int = (SELECT TOP (1) ContextId FROM dbo.Roles WHERE RoleKey = 'Admin');
            IF @ContextId IS NULL THROW 51000, 'Admin permission context must exist before installing WorkTypes.Manage.', 1;
            INSERT dbo.Roles (RoleKey, Name, Description, ContextId, IsSystemRole)
            VALUES ('WorkTypes.Manage', 'Manage Work Types', 'View work types and manage staff work type assignments in the current organisation.', @ContextId, 1);
        END
        """);
    protected override void Down(MigrationBuilder migrationBuilder) => throw new NotSupportedException("Permission assignments must be preserved. Use a reviewed forward migration.");
}
