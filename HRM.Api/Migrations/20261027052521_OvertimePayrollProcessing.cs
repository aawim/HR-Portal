using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class OvertimePayrollProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OT_PayrollBatches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    PayrollMonth = table.Column<DateTime>(type: "date", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ClientBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessedByUserId = table.Column<int>(type: "int", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OT_PayrollBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OT_PayrollBatches_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OT_PayrollBatches_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OT_PayrollBatches_Users_ProcessedByUserId",
                        column: x => x.ProcessedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OT_PayrollItems",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    BatchId = table.Column<long>(type: "bigint", nullable: false),
                    IndividualId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    WorkDate = table.Column<DateTime>(type: "date", nullable: false),
                    Minutes = table.Column<int>(type: "int", nullable: false),
                    RequestReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OT_PayrollItems", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_OT_PayrollItems_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OT_PayrollItems_OT_OvertimeRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "OT_OvertimeRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OT_PayrollItems_OT_PayrollBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "OT_PayrollBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OT_PayrollBatches_OperationLogId",
                table: "OT_PayrollBatches",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_OT_PayrollBatches_OrganisationId_ClientBatchId",
                table: "OT_PayrollBatches",
                columns: new[] { "OrganisationId", "ClientBatchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OT_PayrollBatches_OrganisationId_PayrollMonth",
                table: "OT_PayrollBatches",
                columns: new[] { "OrganisationId", "PayrollMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_OT_PayrollBatches_ProcessedByUserId",
                table: "OT_PayrollBatches",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OT_PayrollItems_BatchId",
                table: "OT_PayrollItems",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_OT_PayrollItems_JobId",
                table: "OT_PayrollItems",
                column: "JobId");
            migrationBuilder.Sql("""
                INSERT dbo.RequestTypesAllowedRequestStateTransitions(RequestTypeID,FromRequestStateID,ToRequestStateID)
                SELECT t.RequestTypeID,e.FromId,e.ToId FROM dbo.RequestTypes t
                CROSS JOIN (VALUES(4,1),(4,5),(4,6),(1,5)) e(FromId,ToId)
                WHERE t.TypeName='Overtime Claim Request'
                AND EXISTS(SELECT 1 FROM dbo.RequestStates WHERE RequestStateID=e.FromId)
                AND EXISTS(SELECT 1 FROM dbo.RequestStates WHERE RequestStateID=e.ToId)
                AND NOT EXISTS(SELECT 1 FROM dbo.RequestTypesAllowedRequestStateTransitions x WHERE x.RequestTypeID=t.RequestTypeID AND x.FromRequestStateID=e.FromId AND x.ToRequestStateID=e.ToId);
                """);
            migrationBuilder.Sql("""
                CREATE OR ALTER TRIGGER dbo.TR_OT_PayrollStateLock ON dbo.Requests AFTER UPDATE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.RequestID=i.RequestID
                        JOIN dbo.OT_OvertimeRequests o ON o.RequestId=i.RequestID
                        WHERE i.RequestStateID<>d.RequestStateID AND
                        (o.PaidAtUtc IS NOT NULL OR EXISTS(SELECT 1 FROM dbo.OT_PayrollItems p WHERE p.RequestId=i.RequestID)))
                        THROW 51020, 'Payroll-processed overtime claims cannot change request status.', 1;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Payroll history must be preserved. Use a reviewed forward migration.");
        }
    }
}
