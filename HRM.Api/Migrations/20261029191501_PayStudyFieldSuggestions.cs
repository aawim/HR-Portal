using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class PayStudyFieldSuggestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PF_StudyFieldSuggestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobFamilyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PF_StudyFieldSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PF_StudyFieldSuggestions_PF_JobFamilies_JobFamilyId",
                        column: x => x.JobFamilyId,
                        principalTable: "PF_JobFamilies",
                        principalColumn: "JobFamilyID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PF_StudyFieldSuggestions_JobFamilyId_Name",
                table: "PF_StudyFieldSuggestions",
                columns: new[] { "JobFamilyId", "Name" },
                unique: true);
            var examples=new System.Collections.Generic.Dictionary<string,string[]>
            {
                ["Business and Administrative Service"]=["Business Administration","Human Resource Management","Accounting","Public Finance","Project Management","Public Relations","Public Policy"],
                ["Teaching Service"]=["Education Management","Curriculum & Instruction","Educational Psychology","Special Education / Inclusive Education","Subject-matter Teaching Degrees"],
                ["Health Service"]=["Medicine (MBBS / MD)","Nursing","Public Health","Pharmacy","Medical Laboratory Sciences","Health Care Administration"],
                ["Information and Communications Service"]=["Computer Science","Software Engineering","Network Administration","Cyber Security","System Architecture","Data Analytics"],
                ["Legal, Cultural and Social Service"]=["Shari'ah & Law","Islamic Studies","Usul al-Deen","Social Work","Counseling","Linguistics","Cultural Heritage Management"],
                ["Science and Built Environment Service"]=["Civil Engineering","Architecture","Quantity Surveying","Environmental Science","Marine Science","Urban Planning"],
                ["Secretarial, Skilled and Elementary Service"]=["Office Administration","Logistics","Marine Operations / Navigation","Mechanical Trades","Technical Vocations"],
                ["Armed Force and Police Service"]=["National Security","Criminology","Intelligence Studies","Specialized Law Enforcement"],
                ["Protective Service"]=["National Security","Criminology","Intelligence Studies","Specialized Law Enforcement"]
            };
            foreach(var family in examples)
            foreach(var field in family.Value)
            {
                var name=field.Replace("'","''");var familyName=family.Key.Replace("'","''");
                migrationBuilder.Sql($"""
                    IF NOT EXISTS(SELECT 1 FROM dbo.PF_JobFamilies WHERE Name=N'{familyName}')
                        THROW 51070,'Import the job family references before study field suggestions.',1;
                    INSERT dbo.PF_StudyFieldSuggestions(JobFamilyId,Name,Source,IsActive)
                    SELECT MIN(JobFamilyID),N'{name}',N'User supplied discipline examples (2026-10-30)',1 FROM dbo.PF_JobFamilies WHERE Name=N'{familyName}';
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new System.NotSupportedException("Preserve reference suggestions and historical configuration. Use a reviewed forward migration.");
        }
    }
}
