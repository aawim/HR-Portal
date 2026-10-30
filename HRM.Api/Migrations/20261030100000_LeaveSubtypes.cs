using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class LeaveSubtypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LT_LeaveSubtypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Days = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CertificateRule = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LT_LeaveSubtypes", x => x.Id);
                    table.CheckConstraint("CK_LT_LeaveSubtypes_Quota", "[Days]>0 AND [CertificateRule] IN ('Any','Required','Without')");
                    table.ForeignKey(
                        name: "FK_LT_LeaveSubtypes_LT_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LT_LeaveTypes",
                        principalColumn: "LeaveTypeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LT_LeaveSubtypes_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LT_LeaveSubtypeUsages",
                columns: table => new
                {
                    LeaveId = table.Column<int>(type: "int", nullable: false),
                    JobLeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    SubtypeId = table.Column<int>(type: "int", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "date", nullable: false),
                    DaysLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SubtypeName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CertificateName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProtectedCertificate = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LT_LeaveSubtypeUsages", x => x.LeaveId);
                    table.ForeignKey(
                        name: "FK_LT_LeaveSubtypeUsages_JobLeaveTypes_JobLeaveTypeId",
                        column: x => x.JobLeaveTypeId,
                        principalTable: "JobLeaveTypes",
                        principalColumn: "JobLeaveTypeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LT_LeaveSubtypeUsages_LT_LeaveSubtypes_SubtypeId",
                        column: x => x.SubtypeId,
                        principalTable: "LT_LeaveSubtypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LT_LeaveSubtypeUsages_Leaves_LeaveId",
                        column: x => x.LeaveId,
                        principalTable: "Leaves",
                        principalColumn: "LeaveID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LT_LeaveSubtypes_LeaveTypeId",
                table: "LT_LeaveSubtypes",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LT_LeaveSubtypes_OrganisationId_LeaveTypeId_Name",
                table: "LT_LeaveSubtypes",
                columns: new[] { "OrganisationId", "LeaveTypeId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LT_LeaveSubtypeUsages_JobLeaveTypeId_SubtypeId_PeriodStart",
                table: "LT_LeaveSubtypeUsages",
                columns: new[] { "JobLeaveTypeId", "SubtypeId", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_LT_LeaveSubtypeUsages_SubtypeId",
                table: "LT_LeaveSubtypeUsages",
                column: "SubtypeId");
            // Enable the requested 15/15 Sick Leave split for the existing NCIT tenant.
            // Historical leave and balances are deliberately not reclassified or reset.
            migrationBuilder.Sql(@"
IF EXISTS(SELECT 1 FROM dbo.Organisations WHERE BusinessEntityID=3)
 AND EXISTS(SELECT 1 FROM dbo.LT_LeaveTypes WHERE LeaveTypeID=9 AND Name=N'Sick Leave' AND Duration=30 AND IsActive=1)
BEGIN
 INSERT dbo.LT_LeaveSubtypes(OrganisationId,LeaveTypeId,Name,Days,CertificateRule,IsActive,Version)
 VALUES(3,9,N'Sick leave with certificate',15,'Required',1,1),
       (3,9,N'Sick leave without certificate',15,'Without',1,1);
END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LT_LeaveSubtypeUsages");

            migrationBuilder.DropTable(
                name: "LT_LeaveSubtypes");
        }
    }
}
