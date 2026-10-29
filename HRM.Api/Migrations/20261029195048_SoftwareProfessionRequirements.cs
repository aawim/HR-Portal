using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SoftwareProfessionRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PF_ProfessionRequirementTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfessionId = table.Column<int>(type: "int", nullable: false),
                    PublicServiceRank = table.Column<int>(type: "int", nullable: false),
                    ProfileJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceReference = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PF_ProfessionRequirementTemplates", x => x.Id);
                    table.CheckConstraint("CK_PF_ProfessionRequirements_Profile", "ISJSON([ProfileJson])=1 AND [PublicServiceRank]>0");
                    table.ForeignKey(
                        name: "FK_PF_ProfessionRequirementTemplates_PF_Professions_ProfessionId",
                        column: x => x.ProfessionId,
                        principalTable: "PF_Professions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PF_ProfessionRequirementTemplates_ProfessionId_PublicServiceRank",
                table: "PF_ProfessionRequirementTemplates",
                columns: new[] { "ProfessionId", "PublicServiceRank" },
                unique: true);
            foreach(var rank in new[]{7,8,9,10})
            {
                var json=System.Text.Json.JsonSerializer.Serialize(SoftwareProfile(rank)).Replace("'","''");
                migrationBuilder.Sql($"""
                    IF (SELECT COUNT(*) FROM dbo.PF_Professions WHERE Name=N'Software and Applications Developers and Analysts')<>1
                        THROW 51075,'An unambiguous software profession reference is required before importing requirements.',1;
                    INSERT dbo.PF_ProfessionRequirementTemplates(ProfessionId,PublicServiceRank,ProfileJson,SourceReference,IsActive)
                    SELECT Id,{rank},N'{json}',N'{Source}',1 FROM dbo.PF_Professions WHERE Name=N'Software and Applications Developers and Analysts';
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new System.NotSupportedException("Preserve source-linked requirement presets. Use a reviewed forward migration.");
        }
        internal const string Source="https://www.finance.gov.mv/public/attachments/rgByNS1JiDA6AFetGJBJ8RE2pfzjYJGx54ouzm5e.pdf";
        internal static HRM.Application.Contracts.Pages.JobMatrixProfile SoftwareProfile(int rank)
        {
            var page=rank switch{10=>1,9=>3,8=>5,7=>7,_=>throw new System.ArgumentOutOfRangeException(nameof(rank))};
            var reference=$"{Source}#page={page}; English working transcription of official Dhivehi software matrix";
            (int Min,int? Max,int Years)[] routes=rank switch
            {
                10=>[(10,null,4),(9,null,8),(7,8,12)],
                9=>[(10,null,0),(9,null,4),(7,8,8)],
                8=>[(9,null,0),(7,8,4)],
                7=>[(7,8,0)],
                _=>throw new System.ArgumentOutOfRangeException(nameof(rank))
            };
            var notes="A licence or completed professional training is required only where the employing organisation specifies it for appointment. The source does not prescribe a universal software practice licence, issuing council, validity period or named certification. Source: "+reference;
            var profile=new HRM.Application.Contracts.Pages.JobMatrixProfile{PublicServiceRank=rank.ToString(),LicensesAndTraining=notes};
            foreach(var route in routes)
            {
                var levels=route.Max.HasValue?$"MNQF Level {route.Min} or {route.Max}":$"MNQF Level {route.Min}";
                var text=$"{levels} in a field related to the job"+(route.Years==0?"":$" AND {route.Years} years of relevant work experience");
                profile.QualificationPathways.Add(new(){Name=text,RelevantExperienceMonths=route.Years*12,SourceReference=reference,Conditions="The qualification and work experience must relate to the job duties. Post-qualification or managerial minimums are not inferred from rank.",Qualifications=[new(){Qualification="Relevant MNQF-recognised qualification",MinimumLevel=route.Min,MaximumLevel=route.Max,FieldOfStudy="A field related to the software job duties",AccreditedRequired=true}]});
            }
            profile.Qualifications=string.Join("\nOR\n",profile.QualificationPathways.ConvertAll(x=>x.Name))+"\nSource: "+reference;
            profile.CredentialRequirements.Add(new(){Name="Licence required by the employing organisation",Kind="Licence",Issuer="Relevant licensing authority specified for the particular requirement",Requirement="Conditional",Condition="Only if the employing organisation explicitly requires a licence for appointment to this job.",VerificationInstructions="Verify the actual licence, issuing authority and validity specified by the recruiting organisation. No named software licence is prescribed by this matrix.",SourceReference=reference});
            profile.CredentialRequirements.Add(new(){Name="Professional training required by the employing organisation",Kind="Training",Issuer="Training provider recognised for the organisation's specified requirement",Requirement="Conditional",Condition="Only if the employing organisation explicitly requires completion of professional training for appointment.",VerificationInstructions="Verify completion of the particular training specified by the employing organisation. No named certification or number of hours is prescribed by this matrix.",SourceReference=reference});
            return profile;
        }
    }
}
