using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HRM.Migrations;

[DbContext(typeof(HrmTeContext))]
[Migration("20261020130000_AddLeaveApprovalUserGroups")]
public class AddLeaveApprovalUserGroups : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_WF_Approvers_Type' AND parent_object_id = OBJECT_ID('dbo.WF_StageApprovers'))
            ALTER TABLE dbo.WF_StageApprovers DROP CONSTRAINT CK_WF_Approvers_Type;
        ALTER TABLE dbo.WF_StageApprovers WITH CHECK ADD CONSTRAINT CK_WF_Approvers_Type
            CHECK (ApproverType IN ('Position','HRHead','HR','HOD','Supervisor','Role','User','UserGroup','SupervisorGroup'));
        """);
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Existing UserGroup-based workflows must remain valid on rollback.
    }
}
