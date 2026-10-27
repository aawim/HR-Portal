using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class StaffPositionChangeRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SP_StaffPositionChangeRequests",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    IndividualId = table.Column<int>(type: "int", nullable: false),
                    PositionId = table.Column<int>(type: "int", nullable: false),
                    ChangeKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FromDate = table.Column<DateTime>(type: "date", nullable: false),
                    ToDate = table.Column<DateTime>(type: "date", nullable: true),
                    OriginalJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_StaffPositionChangeRequests", x => x.RequestId);
                    table.CheckConstraint("CK_SP_PositionRequest_Dates", "[ToDate] IS NULL OR [ToDate] >= [FromDate]");
                    table.CheckConstraint("CK_SP_PositionRequest_Kind", "[ChangeKind] IN ('Assign','Change','Dates','Remove')");
                    table.ForeignKey(
                        name: "FK_SP_StaffPositionChangeRequests_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SP_StaffPositionChangeRequests_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SP_StaffPositionChangeRequests_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "PositionID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SP_StaffPositionChangeRequests_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SP_StaffPositionChangeRequests_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SP_StaffPositionChangeRequests_CreatedByUserId",
                table: "SP_StaffPositionChangeRequests",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_StaffPositionChangeRequests_JobId",
                table: "SP_StaffPositionChangeRequests",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_StaffPositionChangeRequests_OrganisationId_JobId",
                table: "SP_StaffPositionChangeRequests",
                columns: new[] { "OrganisationId", "JobId" });

            migrationBuilder.CreateIndex(
                name: "IX_SP_StaffPositionChangeRequests_PositionId",
                table: "SP_StaffPositionChangeRequests",
                column: "PositionId");
            migrationBuilder.Sql("""
                DECLARE @Types TABLE (Name nvarchar(500), Prefix nvarchar(10));
                INSERT @Types VALUES
                (N'Staff Position Assignment Request',N'SPA'),
                (N'Staff Position Removal Request',N'SPR'),
                (N'Staff Position Change Request',N'SPC'),
                (N'Staff Position Dates Request',N'SPD');
                DECLARE @Name nvarchar(500), @Prefix nvarchar(10), @Sequence int;
                WHILE EXISTS(SELECT 1 FROM @Types)
                BEGIN
                    SELECT TOP(1) @Name=Name,@Prefix=Prefix FROM @Types ORDER BY Name;
                    IF NOT EXISTS(SELECT 1 FROM dbo.RequestTypes WHERE TypeName=@Name)
                    BEGIN
                        INSERT dbo.SequenceNumberTypes(TypeName,Prefix,FormatString,IsResetAnnually,StartingNumber,NextNumber)
                        VALUES(@Name,@Prefix,'{PREFIX}-{YEAR}-{NUMBER}',0,1,1);
                        SET @Sequence=SCOPE_IDENTITY();
                        INSERT dbo.RequestTypes(TypeName,SequenceNumberTypeID,ViewURL,IsSystemType)
                        VALUES(@Name,@Sequence,'/online/staff-position-requests',1);
                    END;
                    DELETE FROM @Types WHERE Name=@Name;
                END;
                IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleKey='Staff.Positions.Manage')
                BEGIN
                    INSERT dbo.OperationLogs(OperationLogActionID,CreatedByContextID,CreatedDate,UpdatedDate,CreatedByIP,UpdatedByIP,Remarks,UpdatedContextID)
                    VALUES(18,1,SYSUTCDATETIME(),SYSUTCDATETIME(),'','','Install staff position request permission.',1);
                    DECLARE @Audit int=SCOPE_IDENTITY();
                    INSERT dbo.Roles(RoleKey,Name,Description,ContextID,IsSystemRole,OperationLogID)
                    VALUES('Staff.Positions.Manage','Staff Positions - Manage','Submit staff position assignment, change, date and removal requests. Approval requires the configured workflow stage.',1,1,@Audit);
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new System.NotSupportedException("Preserve position requests and workflow history; roll back application code without dropping records.");
        }
    }
}
