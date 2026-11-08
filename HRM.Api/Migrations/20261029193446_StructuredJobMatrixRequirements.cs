using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class StructuredJobMatrixRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PF_CredentialRequirements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobMatrixId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Issuer = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Requirement = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Equivalents = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ValidityMonths = table.Column<int>(type: "int", nullable: true),
                    RenewalRequired = table.Column<bool>(type: "bit", nullable: false),
                    TrainingHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    EvidenceRequired = table.Column<bool>(type: "bit", nullable: false),
                    VerificationInstructions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SourceReference = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PF_CredentialRequirements", x => x.Id);
                    table.CheckConstraint("CK_PF_CredentialRequirement_Amounts", "([ValidityMonths] IS NULL OR [ValidityMonths] BETWEEN 1 AND 600) AND ([TrainingHours] IS NULL OR [TrainingHours] BETWEEN 0 AND 10000)");
                    table.CheckConstraint("CK_PF_CredentialRequirement_Kind", "[Kind] IN ('Licence','Registration','Certification','Training')");
                    table.CheckConstraint("CK_PF_CredentialRequirement_Status", "[Requirement] IN ('Mandatory','Preferred','Conditional')");
                    table.ForeignKey(
                        name: "FK_PF_CredentialRequirements_PF_JobMatrix_JobMatrixId",
                        column: x => x.JobMatrixId,
                        principalTable: "PF_JobMatrix",
                        principalColumn: "JobMatrixID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PF_QualificationPathways",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobMatrixId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    RelevantExperienceMonths = table.Column<int>(type: "int", nullable: false),
                    PostQualificationMonths = table.Column<int>(type: "int", nullable: false),
                    ManagerialExperienceMonths = table.Column<int>(type: "int", nullable: false),
                    Conditions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SourceReference = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PF_QualificationPathways", x => x.Id);
                    table.CheckConstraint("CK_PF_QualificationPathway_Months", "[RelevantExperienceMonths] BETWEEN 0 AND 1200 AND [PostQualificationMonths] BETWEEN 0 AND 1200 AND [ManagerialExperienceMonths] BETWEEN 0 AND 1200");
                    table.ForeignKey(
                        name: "FK_PF_QualificationPathways_PF_JobMatrix_JobMatrixId",
                        column: x => x.JobMatrixId,
                        principalTable: "PF_JobMatrix",
                        principalColumn: "JobMatrixID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PF_PathwayQualifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PathwayId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Qualification = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    MinimumLevel = table.Column<int>(type: "int", nullable: true),
                    MaximumLevel = table.Column<int>(type: "int", nullable: true),
                    FieldOfStudy = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AccreditedRequired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PF_PathwayQualifications", x => x.Id);
                    table.CheckConstraint("CK_PF_PathwayQualification_Levels", "([MinimumLevel] IS NULL AND [MaximumLevel] IS NULL) OR ([MinimumLevel] IS NOT NULL AND [MinimumLevel] BETWEEN 1 AND 10 AND ([MaximumLevel] IS NULL OR [MaximumLevel] BETWEEN [MinimumLevel] AND 10))");
                    table.ForeignKey(
                        name: "FK_PF_PathwayQualifications_PF_QualificationPathways_PathwayId",
                        column: x => x.PathwayId,
                        principalTable: "PF_QualificationPathways",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PF_CredentialRequirements_JobMatrixId_IsActive",
                table: "PF_CredentialRequirements",
                columns: new[] { "JobMatrixId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PF_PathwayQualifications_PathwayId_IsActive",
                table: "PF_PathwayQualifications",
                columns: new[] { "PathwayId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PF_QualificationPathways_JobMatrixId_IsActive",
                table: "PF_QualificationPathways",
                columns: new[] { "JobMatrixId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new System.NotSupportedException("Preserve versioned matrix requirements and soft-retired records. Use a reviewed forward migration.");
        }
    }
}
