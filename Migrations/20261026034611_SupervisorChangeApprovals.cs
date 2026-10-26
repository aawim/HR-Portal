using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SupervisorChangeApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SV_SupervisorChangeRequests",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    IndividualId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    TargetKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TargetId = table.Column<int>(type: "int", nullable: false),
                    TargetName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsAssignment = table.Column<bool>(type: "bit", nullable: false),
                    IsOpen = table.Column<bool>(type: "bit", nullable: false),
                    OriginalVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppliedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SV_SupervisorChangeRequests", x => x.RequestId);
                    table.CheckConstraint("CK_SV_SupervisorChangeRequests_Target", "[TargetKind] IN ('Department','Team')");
                    table.ForeignKey(
                        name: "FK_SV_SupervisorChangeRequests_Individuals_IndividualId",
                        column: x => x.IndividualId,
                        principalTable: "Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SV_SupervisorChangeRequests_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SV_SupervisorChangeRequests_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SV_SupervisorChangeRequests_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SV_SupervisorChangeRequests_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SV_SupervisorChangeRequests_CreatedByUserId",
                table: "SV_SupervisorChangeRequests",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SV_SupervisorChangeRequests_IndividualId",
                table: "SV_SupervisorChangeRequests",
                column: "IndividualId");

            migrationBuilder.CreateIndex(
                name: "IX_SV_SupervisorChangeRequests_JobId",
                table: "SV_SupervisorChangeRequests",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_SV_SupervisorChangeRequests_OrganisationId_TargetKind_TargetId_IndividualId",
                table: "SV_SupervisorChangeRequests",
                columns: new[] { "OrganisationId", "TargetKind", "TargetId", "IndividualId" },
                unique: true,
                filter: "[IsOpen] = 1");
            migrationBuilder.Sql("""
                DECLARE @Types TABLE(Name nvarchar(250), Prefix nvarchar(20));
                INSERT @Types VALUES('Supervisor Assignment Request','SVA'),('Supervisor Removal Request','SVR');
                DECLARE @Name nvarchar(250), @Prefix nvarchar(20), @Sequence int;
                DECLARE supervisor_types CURSOR LOCAL FAST_FORWARD FOR SELECT Name,Prefix FROM @Types;
                OPEN supervisor_types; FETCH NEXT FROM supervisor_types INTO @Name,@Prefix;
                WHILE @@FETCH_STATUS=0 BEGIN
                    IF NOT EXISTS(SELECT 1 FROM dbo.RequestTypes WHERE TypeName=@Name)
                    BEGIN
                        INSERT dbo.SequenceNumberTypes(TypeName,Prefix,FormatString,IsResetAnnually,StartingNumber,NextNumber) VALUES(@Name,@Prefix,'{PREFIX}-{YEAR}-{NUMBER}',0,1,1);
                        SET @Sequence=SCOPE_IDENTITY();
                        INSERT dbo.RequestTypes(TypeName,SequenceNumberTypeID,ViewURL,IsSystemType) VALUES(@Name,@Sequence,'/online/supervisor-requests',1);
                    END;
                    FETCH NEXT FROM supervisor_types INTO @Name,@Prefix;
                END;
                CLOSE supervisor_types; DEALLOCATE supervisor_types;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
            => throw new NotSupportedException("Preserve supervisor request and approval history; use a reviewed forward migration for changes.");
    }
}
