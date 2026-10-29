using Microsoft.EntityFrameworkCore.Migrations;

#nullable enable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class PayExperienceAreaSuggestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PF_ExperienceAreaSuggestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobFamilyId = table.Column<int>(type: "int", nullable: true),
                    Sector = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PF_ExperienceAreaSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PF_ExperienceAreaSuggestions_PF_JobFamilies_JobFamilyId",
                        column: x => x.JobFamilyId,
                        principalTable: "PF_JobFamilies",
                        principalColumn: "JobFamilyID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PF_ExperienceAreaSuggestions_JobFamilyId_Sector_Name",
                table: "PF_ExperienceAreaSuggestions",
                columns: new[] { "JobFamilyId", "Sector", "Name" },
                unique: true);
            (string? Family,string Sector,string Name,string Description)[] examples=
            [
                ("Teaching Service","Higher Education & Academic Professionals","Teaching Experience","Formal lecturing and instruction within recognized higher education institutions."),
                ("Teaching Service","Higher Education & Academic Professionals","Research & Publications","Peer-reviewed publications, academic research projects or institutional studies."),
                ("Teaching Service","Higher Education & Academic Professionals","Industry / Practical Field Experience","Direct technical practice relevant to the subject being taught."),
                ("Teaching Service","Higher Education & Academic Professionals","Knowledge Development","Curriculum design, syllabus reform or specialized academic training facilitation."),
                ("Teaching Service","School Education & Educational Administration","Teaching Experience","Formal teaching in early childhood, primary, secondary or higher secondary schools."),
                ("Teaching Service","School Education & Educational Administration","School Management","Administrative or pedagogical leadership, including Leading Teacher, Deputy Principal or Principal."),
                ("Teaching Service","School Education & Educational Administration","Education Policy & Mandate","Technical, managerial or policy functions in education ministries or oversight agencies."),
                (null,"Functional & Corporate Professionals","Administrative Operations","Office operations, human resources management, public relations or registry governance."),
                (null,"Functional & Corporate Professionals","Financial Management","State accounting, budget control, audit functions or public procurement."),
                (null,"Functional & Corporate Professionals","Institutional Governance","Legal counsel, organizational quality assurance, statutory compliance or public policy planning."),
                ("Health Service","Health & Medical Services","Clinical Practice","Direct patient care, specialized nursing, clinical diagnostics or medical consultations."),
                ("Health Service","Health & Medical Services","Health Policy & Administration","Health data management, regulatory compliance, epidemiological surveillance or health system design."),
                ("Health Service","Health & Medical Services","Allied Health Practice","Laboratory analysis, pharmacy operations, radiography or physiotherapy services."),
                ("Information and Communications Service","ICT & Technical Services","Systems Architecture & Development","Software development, database management and platform integration."),
                ("Information and Communications Service","ICT & Technical Services","Network & Cyber Infrastructure","Systems administration, cybersecurity operations and network engineering."),
                ("Information and Communications Service","ICT & Technical Services","Technical Support","Infrastructure maintenance, systems troubleshooting and end-user hardware operations.")
            ];
            foreach(var row in examples)
            {
                static string Quote(string s)=>"N'"+s.Replace("'","''")+"'";
                var familyId=row.Family==null?"NULL":$"(SELECT MIN(JobFamilyID) FROM dbo.PF_JobFamilies WHERE Name={Quote(row.Family)})";
                if(row.Family!=null)migrationBuilder.Sql($"IF NOT EXISTS(SELECT 1 FROM dbo.PF_JobFamilies WHERE Name={Quote(row.Family)}) THROW 51071,'Import job families before experience suggestions.',1;");
                migrationBuilder.Sql($"INSERT dbo.PF_ExperienceAreaSuggestions(JobFamilyId,Sector,Name,Description,Source,IsActive) VALUES({familyId},{Quote(row.Sector)},{Quote(row.Name)},{Quote(row.Description)},N'User supplied sector examples (2026-10-30)',1);");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new System.NotSupportedException("Preserve reference suggestions. Use a reviewed forward migration.");
        }
    }
}
