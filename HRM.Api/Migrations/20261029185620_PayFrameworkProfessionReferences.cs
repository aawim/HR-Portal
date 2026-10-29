using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class PayFrameworkProfessionReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PF_Professions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobFamilyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Tasks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExampleTitlesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MisscoCodesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceEdition = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourcePage = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PF_Professions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PF_Professions_PF_JobFamilies_JobFamilyId",
                        column: x => x.JobFamilyId,
                        principalTable: "PF_JobFamilies",
                        principalColumn: "JobFamilyID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PF_Professions_JobFamilyId_Name_SourceEdition",
                table: "PF_Professions",
                columns: new[] { "JobFamilyId", "Name", "SourceEdition" },
                unique: true);
            migrationBuilder.Sql("""
                ALTER TABLE dbo.PF_JobMatrix ADD ProfessionID int NULL;
                ALTER TABLE dbo.PF_JobMatrix WITH CHECK ADD CONSTRAINT FK_PF_JobMatrix_Profession FOREIGN KEY(ProfessionID) REFERENCES dbo.PF_Professions(Id);
                """);
            using var stream=typeof(PayFrameworkProfessionReferences).Assembly.GetManifestResourceStream("HRM.Reference.JobFamilyModel2019.json")??throw new System.InvalidOperationException("Job family reference data is missing.");
            using var reader=new System.IO.StreamReader(stream);
            var json=reader.ReadToEnd().Replace("'","''");
            migrationBuilder.Sql($"""
                DECLARE @source nvarchar(max)=N'{json}';
                INSERT dbo.PF_JobFamilies(Name,IsActive)
                SELECT DISTINCT s.Family,1 FROM OPENJSON(@source) WITH(Family nvarchar(250) '$.family') s
                WHERE NOT EXISTS(SELECT 1 FROM dbo.PF_JobFamilies f WHERE f.Name=s.Family);
                INSERT dbo.PF_Professions(JobFamilyId,Name,Tasks,ExampleTitlesJson,MisscoCodesJson,SourceEdition,SourcePage,IsActive)
                SELECT (SELECT MIN(f.JobFamilyID) FROM dbo.PF_JobFamilies f WHERE f.Name=s.Family),s.Name,s.Tasks,s.Titles,s.Codes,N'NPC Job Family Model 2019',s.Page,1
                FROM OPENJSON(@source) WITH(Family nvarchar(250) '$.family',Name nvarchar(250) '$.name',Tasks nvarchar(max) '$.tasks',Titles nvarchar(max) '$.titles' AS JSON,Codes nvarchar(max) '$.codes' AS JSON,Page int '$.page') s;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new System.NotSupportedException("Preserve imported reference records and linked job matrices. Use a reviewed forward migration.");
        }
    }
}
