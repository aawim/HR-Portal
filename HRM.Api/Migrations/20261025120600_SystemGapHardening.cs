using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SystemGapHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            using var stream=typeof(SystemGapHardening).Assembly.GetManifestResourceStream("HRM.Database.Scripts.20261025_LegacySchemaCompatibility.sql") ?? throw new InvalidOperationException("Legacy compatibility script missing.");
            using var reader=new System.IO.StreamReader(stream);
            migrationBuilder.Sql(reader.ReadToEnd());
            migrationBuilder.CreateTable(
                name: "OT_OvertimeRequests",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    IndividualId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    WorkDate = table.Column<DateTime>(type: "date", nullable: false),
                    WeekStart = table.Column<DateTime>(type: "date", nullable: false),
                    Minutes = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ApprovedPolicyJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidByUserId = table.Column<int>(type: "int", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OT_OvertimeRequests", x => x.RequestId);
                    table.CheckConstraint("CK_OT_OvertimeRequests_Minutes", "[Minutes] > 0 AND [Minutes] <= 1440");
                    table.ForeignKey(
                        name: "FK_OT_OvertimeRequests_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OT_OvertimeRequests_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OT_OvertimeRequests_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OT_OvertimeRequests_Users_PaidByUserId",
                        column: x => x.PaidByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OT_OvertimeRequests_JobId",
                table: "OT_OvertimeRequests",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_OT_OvertimeRequests_OrganisationId_IndividualId_WorkDate",
                table: "OT_OvertimeRequests",
                columns: new[] { "OrganisationId", "IndividualId", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OT_OvertimeRequests_PaidByUserId",
                table: "OT_OvertimeRequests",
                column: "PaidByUserId");
            migrationBuilder.Sql("""
                IF NOT EXISTS(SELECT 1 FROM dbo.RequestTypes WHERE TypeName='Overtime Claim Request')
                BEGIN
                    INSERT dbo.SequenceNumberTypes(TypeName,Prefix,FormatString,IsResetAnnually,StartingNumber,NextNumber) VALUES('Overtime Claims','OT','{PREFIX}-{YEAR}-{NUMBER}',0,1,1);
                    DECLARE @Sequence int=SCOPE_IDENTITY();
                    INSERT dbo.RequestTypes(TypeName,SequenceNumberTypeID,ViewURL,IsSystemType) VALUES('Overtime Claim Request',@Sequence,'/online/overtime-requests',1);
                END;
                DECLARE @Keys TABLE(RoleKey nvarchar(200),Name nvarchar(250));
                INSERT @Keys VALUES('Leave.Configuration.Manage','Leave Configuration - Manage'),('Individual.Add','Individual - Register'),('Overtime.Review','Overtime - Review'),('Overtime.Pay','Overtime - Record payment');
                IF EXISTS(SELECT 1 FROM @Keys k WHERE NOT EXISTS(SELECT 1 FROM dbo.Roles r WHERE r.RoleKey=k.RoleKey))
                BEGIN
                    INSERT dbo.OperationLogs(OperationLogActionID,CreatedByContextID,CreatedDate,UpdatedDate,CreatedByIP,UpdatedByIP,Remarks,UpdatedContextID) VALUES(18,1,SYSUTCDATETIME(),SYSUTCDATETIME(),'','','Install explicit registry, leave configuration and overtime permissions; assignments remain subject to organisation permission limits.',1);
                    DECLARE @Audit int=SCOPE_IDENTITY();
                    INSERT dbo.Roles(RoleKey,Name,Description,ContextID,IsSystemRole,OperationLogID) SELECT k.RoleKey,k.Name,k.Name,1,1,@Audit FROM @Keys k WHERE NOT EXISTS(SELECT 1 FROM dbo.Roles r WHERE r.RoleKey=k.RoleKey);
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Preserve overtime claims, legacy lookup records and audit history; roll back application code without dropping data.");
        }
    }
}
