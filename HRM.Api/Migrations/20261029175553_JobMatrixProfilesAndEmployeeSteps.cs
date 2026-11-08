using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class JobMatrixProfilesAndEmployeeSteps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "JobMatrixStepId",
                table: "SP_StaffPositionChangeRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JobMatrixStepId",
                table: "SO_StaffOnboardingRequests",
                type: "int",
                nullable: true);
            migrationBuilder.Sql("""
                -- Retain existing position-step values solely for historical compatibility.
                -- New position matrices do not select a step; approved employee Jobs do.
                ALTER TABLE dbo.PF_PositionMatrix ALTER COLUMN JobMatrixStepID int NULL;
                IF COL_LENGTH('dbo.PF_JobMatrix','RevisedDate') IS NULL ALTER TABLE dbo.PF_JobMatrix ADD RevisedDate date NULL;
                IF COL_LENGTH('dbo.PF_JobMatrix','JobFamilyName') IS NULL ALTER TABLE dbo.PF_JobMatrix ADD JobFamilyName nvarchar(250) NULL;
                IF COL_LENGTH('dbo.PF_JobMatrix','MisscoCode') IS NULL ALTER TABLE dbo.PF_JobMatrix ADD MisscoCode nvarchar(10) NULL;
                IF COL_LENGTH('dbo.PF_JobMatrix','SkillLevel') IS NULL ALTER TABLE dbo.PF_JobMatrix ADD SkillLevel tinyint NULL;
                IF COL_LENGTH('dbo.PF_JobMatrix','LicensesAndTraining') IS NULL ALTER TABLE dbo.PF_JobMatrix ADD LicensesAndTraining nvarchar(max) NULL;
                IF COL_LENGTH('dbo.PF_JobMatrix','FieldsOfStudy') IS NULL ALTER TABLE dbo.PF_JobMatrix ADD FieldsOfStudy nvarchar(max) NULL;
                IF COL_LENGTH('dbo.PF_JobMatrix','ExperienceAreas') IS NULL ALTER TABLE dbo.PF_JobMatrix ADD ExperienceAreas nvarchar(max) NULL;
                ALTER TABLE dbo.SP_StaffPositionChangeRequests WITH CHECK ADD CONSTRAINT FK_SP_PositionRequest_JobMatrixStep FOREIGN KEY(JobMatrixStepId) REFERENCES dbo.PF_JobMatrixSteps(JobMatrixStepID);
                ALTER TABLE dbo.SO_StaffOnboardingRequests WITH CHECK ADD CONSTRAINT FK_SO_Onboarding_JobMatrixStep FOREIGN KEY(JobMatrixStepId) REFERENCES dbo.PF_JobMatrixSteps(JobMatrixStepID);
                """);
            migrationBuilder.Sql("""
                UPDATE m SET JobFamilyName=f.Name FROM dbo.PF_JobMatrix m
                JOIN dbo.PF_JobFamilies f ON f.JobFamilyID=m.JobFamilyID WHERE m.JobFamilyName IS NULL;
                UPDATE m SET MisscoCode=g.Code,SkillLevel=g.SkillLevel FROM dbo.PF_JobMatrix m
                JOIN dbo.PF_MisscoGroups g ON g.MisscoGroupID=m.MisscoGroupID WHERE m.MisscoCode IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new System.NotSupportedException("Preserve Job Matrix profiles, request step selections and employment pay history. Use a reviewed forward migration.");
        }
    }
}
