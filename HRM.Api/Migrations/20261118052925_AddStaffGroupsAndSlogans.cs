using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffGroupsAndSlogans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SG_StaffGroups",
                columns: table => new
                {
                    StaffGroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WorkTemplateId = table.Column<int>(type: "int", nullable: true),
                    ScheduleMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    WorkingDaysMask = table.Column<int>(type: "int", nullable: false),
                    WorkTypeId = table.Column<int>(type: "int", nullable: true),
                    OvertimeEnabled = table.Column<bool>(type: "bit", nullable: true),
                    MaximumDailyOvertimeHours = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    MaximumWeeklyOvertimeHours = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    OvertimeRateMultiplier = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    LeaveSetId = table.Column<int>(type: "int", nullable: true),
                    AdminPortalAccessEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDissolved = table.Column<bool>(type: "bit", nullable: false),
                    DissolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DissolvedByUserId = table.Column<int>(type: "int", nullable: true),
                    DissolveReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OperationLogId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SG_StaffGroups", x => x.StaffGroupId);
                    table.ForeignKey(
                        name: "FK_SG_StaffGroups_LeaveSets_LeaveSetId",
                        column: x => x.LeaveSetId,
                        principalTable: "LeaveSets",
                        principalColumn: "LeaveSetID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SG_StaffGroups_WorkTemplates_WorkTemplateId",
                        column: x => x.WorkTemplateId,
                        principalTable: "WorkTemplates",
                        principalColumn: "WorkTemplateID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SG_StaffGroups_WorkTypes_WorkTypeId",
                        column: x => x.WorkTypeId,
                        principalTable: "WorkTypes",
                        principalColumn: "WorkTypeID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SL_Slogans",
                columns: table => new
                {
                    SloganId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SloganType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TextContent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TextColor = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    BackgroundColor = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    BorderColor = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    BoxStyle = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ImageData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ImageContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Placement = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Alignment = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OperationLogId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SL_Slogans", x => x.SloganId);
                });

            migrationBuilder.CreateTable(
                name: "SG_StaffGroupMembers",
                columns: table => new
                {
                    StaffGroupMemberId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffGroupId = table.Column<int>(type: "int", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    IndividualId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    JoinedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeftAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    HadDirectAdminPortalAccess = table.Column<bool>(type: "bit", nullable: false),
                    GrantedAdminPortalAccess = table.Column<bool>(type: "bit", nullable: false),
                    AddedByUserId = table.Column<int>(type: "int", nullable: false),
                    RemovedByUserId = table.Column<int>(type: "int", nullable: true),
                    OperationLogId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SG_StaffGroupMembers", x => x.StaffGroupMemberId);
                    table.ForeignKey(
                        name: "FK_SG_StaffGroupMembers_SG_StaffGroups_StaffGroupId",
                        column: x => x.StaffGroupId,
                        principalTable: "SG_StaffGroups",
                        principalColumn: "StaffGroupId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SL_DocumentSlogans",
                columns: table => new
                {
                    DocumentSloganAssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    SloganId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SL_DocumentSlogans", x => x.DocumentSloganAssignmentId);
                    table.ForeignKey(
                        name: "FK_SL_DocumentSlogans_SL_Slogans_SloganId",
                        column: x => x.SloganId,
                        principalTable: "SL_Slogans",
                        principalColumn: "SloganId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SG_StaffGroupMembers_ActiveIndividual",
                table: "SG_StaffGroupMembers",
                columns: new[] { "OrganisationId", "IndividualId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SG_StaffGroupMembers_StaffGroupId_IsActive",
                table: "SG_StaffGroupMembers",
                columns: new[] { "StaffGroupId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SG_StaffGroups_LeaveSetId",
                table: "SG_StaffGroups",
                column: "LeaveSetId");

            migrationBuilder.CreateIndex(
                name: "IX_SG_StaffGroups_OrganisationId_IsActive_IsDissolved",
                table: "SG_StaffGroups",
                columns: new[] { "OrganisationId", "IsActive", "IsDissolved" });

            migrationBuilder.CreateIndex(
                name: "IX_SG_StaffGroups_WorkTemplateId",
                table: "SG_StaffGroups",
                column: "WorkTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SG_StaffGroups_WorkTypeId",
                table: "SG_StaffGroups",
                column: "WorkTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SL_DocumentSlogans_OrganisationId_DocumentType",
                table: "SL_DocumentSlogans",
                columns: new[] { "OrganisationId", "DocumentType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SL_DocumentSlogans_SloganId",
                table: "SL_DocumentSlogans",
                column: "SloganId");

            migrationBuilder.CreateIndex(
                name: "IX_SL_Slogans_OrganisationId_IsActive_IsDefault",
                table: "SL_Slogans",
                columns: new[] { "OrganisationId", "IsActive", "IsDefault" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SG_StaffGroupMembers");

            migrationBuilder.DropTable(
                name: "SL_DocumentSlogans");

            migrationBuilder.DropTable(
                name: "SG_StaffGroups");

            migrationBuilder.DropTable(
                name: "SL_Slogans");
        }
    }
}
