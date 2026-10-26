using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class OvertimeWorkRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClaimSource",
                table: "OT_OvertimeRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Legacy");

            migrationBuilder.AddColumn<int>(
                name: "EndLogId",
                table: "OT_OvertimeRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndedAt",
                table: "OT_OvertimeRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreapprovalTaskId",
                table: "OT_OvertimeRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StartLogId",
                table: "OT_OvertimeRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "OT_OvertimeRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OT_WorkRequests",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    IndividualId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FromDate = table.Column<DateTime>(type: "date", nullable: false),
                    ToDate = table.Column<DateTime>(type: "date", nullable: false),
                    ProposedStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProposedEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartLogId = table.Column<int>(type: "int", nullable: true),
                    EndLogId = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OT_WorkRequests", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_OT_WorkRequests_AttendanceLogs_EndLogId",
                        column: x => x.EndLogId,
                        principalTable: "AttendanceLogs",
                        principalColumn: "AttendanceLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OT_WorkRequests_AttendanceLogs_StartLogId",
                        column: x => x.StartLogId,
                        principalTable: "AttendanceLogs",
                        principalColumn: "AttendanceLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OT_WorkRequests_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OT_WorkRequests_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OT_WorkRequests_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OT_WorkTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkRequestId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OT_WorkTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OT_WorkTasks_OT_WorkRequests_WorkRequestId",
                        column: x => x.WorkRequestId,
                        principalTable: "OT_WorkRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OT_OvertimeRequests_EndLogId",
                table: "OT_OvertimeRequests",
                column: "EndLogId");

            migrationBuilder.CreateIndex(
                name: "IX_OT_OvertimeRequests_PreapprovalTaskId",
                table: "OT_OvertimeRequests",
                column: "PreapprovalTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_OT_OvertimeRequests_StartLogId",
                table: "OT_OvertimeRequests",
                column: "StartLogId");

            migrationBuilder.CreateIndex(
                name: "IX_OT_WorkRequests_EndLogId",
                table: "OT_WorkRequests",
                column: "EndLogId");

            migrationBuilder.CreateIndex(
                name: "IX_OT_WorkRequests_JobId",
                table: "OT_WorkRequests",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_OT_WorkRequests_OrganisationId_IndividualId_FromDate",
                table: "OT_WorkRequests",
                columns: new[] { "OrganisationId", "IndividualId", "FromDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OT_WorkRequests_StartLogId",
                table: "OT_WorkRequests",
                column: "StartLogId");

            migrationBuilder.CreateIndex(
                name: "IX_OT_WorkTasks_WorkRequestId",
                table: "OT_WorkTasks",
                column: "WorkRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_OT_OvertimeRequests_AttendanceLogs_EndLogId",
                table: "OT_OvertimeRequests",
                column: "EndLogId",
                principalTable: "AttendanceLogs",
                principalColumn: "AttendanceLogID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OT_OvertimeRequests_AttendanceLogs_StartLogId",
                table: "OT_OvertimeRequests",
                column: "StartLogId",
                principalTable: "AttendanceLogs",
                principalColumn: "AttendanceLogID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OT_OvertimeRequests_OT_WorkTasks_PreapprovalTaskId",
                table: "OT_OvertimeRequests",
                column: "PreapprovalTaskId",
                principalTable: "OT_WorkTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM dbo.RequestTypes WHERE TypeName=N'Overtime Preapproval Request')
                BEGIN
                    INSERT dbo.SequenceNumberTypes(TypeName,Prefix,FormatString,IsResetAnnually,StartingNumber,NextNumber)
                    VALUES(N'Overtime Preapprovals',N'OTP',N'{PREFIX}-{YEAR}-{NUMBER}',0,1,1);
                    DECLARE @PreSequence int=SCOPE_IDENTITY();
                    INSERT dbo.RequestTypes(TypeName,SequenceNumberTypeID,ViewURL,IsSystemType)
                    VALUES(N'Overtime Preapproval Request',@PreSequence,N'/online/overtime-requests',1);
                END;
                IF NOT EXISTS (SELECT 1 FROM dbo.RequestTypes WHERE TypeName=N'Overtime Clock Event Request')
                BEGIN
                    INSERT dbo.SequenceNumberTypes(TypeName,Prefix,FormatString,IsResetAnnually,StartingNumber,NextNumber)
                    VALUES(N'Overtime Clock Events',N'OTC',N'{PREFIX}-{YEAR}-{NUMBER}',0,1,1);
                    DECLARE @ClockSequence int=SCOPE_IDENTITY();
                    INSERT dbo.RequestTypes(TypeName,SequenceNumberTypeID,ViewURL,IsSystemType)
                    VALUES(N'Overtime Clock Event Request',@ClockSequence,N'/online/overtime-requests',1);
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("OT requests and clock evidence must be retained. Restore the reviewed pre-upgrade backup to roll back.");
        }
    }
}
