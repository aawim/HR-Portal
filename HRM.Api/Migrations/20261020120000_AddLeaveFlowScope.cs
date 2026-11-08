using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HRM.Migrations;

[DbContext(typeof(HrmTeContext))]
[Migration("20261020120000_AddLeaveFlowScope")]
public class AddLeaveFlowScope : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        IF COL_LENGTH('dbo.WF_WorkflowDefinitions', 'LeaveTypeId') IS NULL
            ALTER TABLE dbo.WF_WorkflowDefinitions ADD LeaveTypeId int NULL;
        IF COL_LENGTH('dbo.WF_WorkflowDefinitions', 'LeaveRequestKind') IS NULL
            ALTER TABLE dbo.WF_WorkflowDefinitions ADD LeaveRequestKind int NULL;
        """);
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("LeaveTypeId", "WF_WorkflowDefinitions");
        migrationBuilder.DropColumn("LeaveRequestKind", "WF_WorkflowDefinitions");
    }
}
