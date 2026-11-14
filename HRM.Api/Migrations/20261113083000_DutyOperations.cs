using System;
using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace HRM.Migrations;
[DbContext(typeof(HrmTeContext))]
[Migration("20261113083000_DutyOperations")]
public partial class DutyOperations : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("WS_DutyOperations", t => new { Id=t.Column<long>(nullable:false).Annotation("SqlServer:Identity","1, 1"), DutyAssignmentId=t.Column<long>(nullable:false), OperationType=t.Column<string>(maxLength:40,nullable:false), Status=t.Column<string>(maxLength:30,nullable:false), ReplacementDutyAssignmentId=t.Column<long>(nullable:true), ActualStartAt=t.Column<DateTime>(nullable:true), ActualEndAt=t.Column<DateTime>(nullable:true), Reason=t.Column<string>(maxLength:1000,nullable:false), RequestedByUserId=t.Column<int>(nullable:false), RequestedAtUtc=t.Column<DateTime>(nullable:false), RequestId=t.Column<int>(nullable:true), OperationLogId=t.Column<int>(nullable:false) }, t=>{t.PrimaryKey("PK_WS_DutyOperations",x=>x.Id);t.CheckConstraint("CK_WS_DutyOperations_ActualTimes","[ActualEndAt] IS NULL OR [ActualStartAt] IS NULL OR [ActualEndAt] >= [ActualStartAt]");t.ForeignKey("FK_WS_DutyOperations_WS_DutyAssignments",x=>x.DutyAssignmentId,"WS_DutyAssignments","Id",onDelete:ReferentialAction.Restrict);t.ForeignKey("FK_WS_DutyOperations_WS_DutyAssignments_Replacement",x=>x.ReplacementDutyAssignmentId,"WS_DutyAssignments","Id",onDelete:ReferentialAction.Restrict);t.ForeignKey("FK_WS_DutyOperations_Users",x=>x.RequestedByUserId,"Users","UserID",onDelete:ReferentialAction.Restrict);t.ForeignKey("FK_WS_DutyOperations_Requests",x=>x.RequestId,"Requests","RequestID",onDelete:ReferentialAction.Restrict);t.ForeignKey("FK_WS_DutyOperations_OperationLogs",x=>x.OperationLogId,"OperationLogs","OperationLogID",onDelete:ReferentialAction.Restrict);});
        m.CreateIndex("IX_WS_DutyOperations_DutyAssignmentId_Status","WS_DutyOperations",new[]{"DutyAssignmentId","Status"});
    }
    protected override void Down(MigrationBuilder m)=>m.DropTable("WS_DutyOperations");
}
