using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruitmentApplicantLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RM_RecruitmentApplicationDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    StoredObjectId = table.Column<long>(type: "bigint", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RM_RecruitmentApplicationDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentApplicationDocuments_DMS_StoredObjects_StoredObjectId",
                        column: x => x.StoredObjectId,
                        principalTable: "DMS_StoredObjects",
                        principalColumn: "StoredObjectId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentApplicationDocuments_RM_RecruitmentApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "RM_RecruitmentApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RM_RecruitmentApplicationEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RM_RecruitmentApplicationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentApplicationEvents_RM_RecruitmentApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "RM_RecruitmentApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentApplicationDocuments_ApplicationId_StoredObjectId",
                table: "RM_RecruitmentApplicationDocuments",
                columns: new[] { "ApplicationId", "StoredObjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentApplicationDocuments_StoredObjectId",
                table: "RM_RecruitmentApplicationDocuments",
                column: "StoredObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentApplicationEvents_ApplicationId_CreatedAtUtc",
                table: "RM_RecruitmentApplicationEvents",
                columns: new[] { "ApplicationId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RM_RecruitmentApplicationDocuments");

            migrationBuilder.DropTable(
                name: "RM_RecruitmentApplicationEvents");
        }
    }
}
