using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruitmentOfficialTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RM_RecruitmentOfficialTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecruitmentPolicyVersionId = table.Column<int>(type: "int", nullable: false),
                    SourceDocumentId = table.Column<long>(type: "bigint", nullable: false),
                    TemplateCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    SourceSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TemplateDefinitionJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    PositionCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    VerificationStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RM_RecruitmentOfficialTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentOfficialTemplates_DMS_Documents_SourceDocumentId",
                        column: x => x.SourceDocumentId,
                        principalTable: "DMS_Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RM_RecruitmentOfficialTemplates_RM_RecruitmentPolicyVersions_RecruitmentPolicyVersionId",
                        column: x => x.RecruitmentPolicyVersionId,
                        principalTable: "RM_RecruitmentPolicyVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentOfficialTemplates_RecruitmentPolicyVersionId_TemplateCode_PositionCategory",
                table: "RM_RecruitmentOfficialTemplates",
                columns: new[] { "RecruitmentPolicyVersionId", "TemplateCode", "PositionCategory" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RM_RecruitmentOfficialTemplates_SourceDocumentId",
                table: "RM_RecruitmentOfficialTemplates",
                column: "SourceDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RM_RecruitmentOfficialTemplates");
        }
    }
}
