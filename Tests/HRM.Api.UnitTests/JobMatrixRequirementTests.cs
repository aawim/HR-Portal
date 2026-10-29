using HRM.Application.Contracts.Pages;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace HRM.Api.UnitTests;
public sealed partial class PayFrameworkTests
{
    private static JobMatrixInput WithRequirements(string code="REQ")
    {
        var input=Input(code);
        return input with{Profile=input.Profile with{
            LicensesAndTraining="Legacy licensing notes",Qualifications="Legacy qualification notes",
            CredentialRequirements=[new(){Name="Practice licence",Issuer="Example council",ValidityMonths=12,RenewalRequired=true,SourceReference="TEST-RULE"}],
            QualificationPathways=[new(){Name="Primary route",RelevantExperienceMonths=96,PostQualificationMonths=48,ManagerialExperienceMonths=24,Qualifications=[new(){Qualification="Bachelor",MinimumLevel=7},new(){Qualification="Master",MinimumLevel=9},new(){Qualification="Doctorate",MinimumLevel=10}]},new(){Name="Alternative route",RelevantExperienceMonths=120,Qualifications=[new(){Qualification="Related degree",MinimumLevel=7,MaximumLevel=8,FieldOfStudy="Related discipline"}]}]
        }};
    }
    [Fact]public async Task Structured_credentials_and_and_or_pathways_roundtrip_without_changing_legacy_notes()
    {
        await Seed();Assert.True((await Service.SaveJobMatrixAsync(WithRequirements())).Success);
        var matrix=Assert.Single((await Service.LoadPayFrameworksAsync()).Matrices);var profile=matrix.Profile;
        var credential=Assert.Single(profile.CredentialRequirements);Assert.True(credential.Id>0);Assert.Equal(12,credential.ValidityMonths);Assert.True(credential.RenewalRequired);
        Assert.Equal(2,profile.QualificationPathways.Count);Assert.Equal(new int?[]{7,9,10},profile.QualificationPathways[0].Qualifications.Select(x=>x.MinimumLevel));Assert.Equal(8,profile.QualificationPathways[1].Qualifications[0].MaximumLevel);
        Assert.Equal("Legacy licensing notes",profile.LicensesAndTraining);Assert.Equal("Legacy qualification notes",profile.Qualifications);Assert.NotEmpty(profile.RequirementsVersion);
        await using var db=factory.CreateDbContext();Assert.Equal(2,await db.MatrixQualificationPathways.CountAsync());Assert.Equal(4,await db.MatrixQualificationConditions.CountAsync());
    }
    [Fact]public async Task Unused_requirement_updates_keep_ids_and_removals_are_soft_and_stale_edits_are_blocked()
    {
        await Seed();var input=WithRequirements();await Service.SaveJobMatrixAsync(input);var original=Assert.Single((await Service.LoadPayFrameworksAsync()).Matrices);
        var profile=original.Profile with{CredentialRequirements=[original.Profile.CredentialRequirements[0] with{Name="Updated practice licence"}],QualificationPathways=[original.Profile.QualificationPathways[0] with{Qualifications=[original.Profile.QualificationPathways[0].Qualifications[0]]}]};
        var result=await Service.SaveJobMatrixAsync(input with{Id=original.Id,Profile=profile});Assert.True(result.Success,result.Message);
        Assert.False((await Service.SaveJobMatrixAsync(input with{Id=original.Id,Profile=original.Profile})).Success);
        var updated=Assert.Single((await Service.LoadPayFrameworksAsync()).Matrices);Assert.Equal(original.Profile.CredentialRequirements[0].Id,updated.Profile.CredentialRequirements[0].Id);Assert.Equal("Updated practice licence",updated.Profile.CredentialRequirements[0].Name);
        await using var db=factory.CreateDbContext();Assert.Equal(2,await db.MatrixQualificationPathways.CountAsync());Assert.Equal(1,await db.MatrixQualificationPathways.CountAsync(x=>x.IsActive));Assert.Equal(4,await db.MatrixQualificationConditions.CountAsync());Assert.Equal(1,await db.MatrixQualificationConditions.CountAsync(x=>x.IsActive));
        Assert.Single(db.FrameworkMatrixQualifications);Assert.Equal("Legacy qualification notes",(await db.FrameworkMatrixQualifications.SingleAsync()).RequirementText);
    }
    [Fact]public async Task Requirement_ids_cannot_be_borrowed_from_another_matrix()
    {
        await Seed();await Service.SaveJobMatrixAsync(WithRequirements("A"));await Service.SaveJobMatrixAsync(WithRequirements("B"));
        var rows=(await Service.LoadPayFrameworksAsync()).Matrices;var a=rows.Single(x=>x.Code=="A");var b=rows.Single(x=>x.Code=="B");
        Assert.False((await Service.SaveJobMatrixAsync(WithRequirements("A") with{Id=a.Id,Profile=a.Profile with{CredentialRequirements=b.Profile.CredentialRequirements}})).Success);
        Assert.False((await Service.SaveJobMatrixAsync(WithRequirements("A") with{Id=a.Id,Profile=a.Profile with{QualificationPathways=b.Profile.QualificationPathways}})).Success);
    }
    [Theory]
    [InlineData("name","Credential requirement 1: enter a credential")]
    [InlineData("issuer","Credential requirement 1: enter the issuing")]
    [InlineData("condition","Credential requirement 1: enter a condition")]
    [InlineData("validity","Credential requirement 1: validity")]
    [InlineData("training","Credential requirement 1: training hours")]
    [InlineData("levels","Qualification pathway 1: qualification 1: the upper MNQF level")]
    [InlineData("missing-minimum","Qualification pathway 1: qualification 1: select an MNQF level")]
    [InlineData("out-of-range","Qualification pathway 1: qualification 1: MNQF levels must")]
    [InlineData("qualification","Qualification pathway 1: qualification 1: enter a qualification")]
    [InlineData("months","Qualification pathway 1: post-qualification experience")]
    [InlineData("relevant-months","Qualification pathway 1: relevant experience")]
    [InlineData("managerial-months","Qualification pathway 1: managerial experience")]
    public async Task Invalid_structured_requirements_do_not_create_a_matrix(string error,string expectedMessage)
    {
        await Seed();var input=WithRequirements();var p=input.Profile;
        switch(error)
        {
            case "name":p.CredentialRequirements[0].Name="";break;
            case "issuer":p.CredentialRequirements[0].Issuer="";break;
            case "condition":p.CredentialRequirements[0].Requirement="Conditional";break;
            case "validity":p.CredentialRequirements[0].ValidityMonths=0;break;
            case "training":p.CredentialRequirements[0].TrainingHours=-1;break;
            case "levels":p.QualificationPathways[0].Qualifications[0].MaximumLevel=6;break;
            case "missing-minimum":p.QualificationPathways[0].Qualifications[0].MinimumLevel=null;p.QualificationPathways[0].Qualifications[0].MaximumLevel=8;break;
            case "months":p.QualificationPathways[0].PostQualificationMonths=-1;break;
            case "out-of-range":p.QualificationPathways[0].Qualifications[0].MinimumLevel=11;break;
            case "qualification":p.QualificationPathways[0].Qualifications[0].Qualification="";break;
            case "relevant-months":p.QualificationPathways[0].RelevantExperienceMonths=1201;break;
            case "managerial-months":p.QualificationPathways[0].ManagerialExperienceMonths=-1;break;
        }
        var result=await Service.SaveJobMatrixAsync(input);Assert.False(result.Success);Assert.StartsWith(expectedMessage,result.Message);await using var db=factory.CreateDbContext();Assert.Empty(db.FrameworkJobMatrices);
    }
    [Fact]public async Task Referenced_matrix_requirements_are_frozen_and_copying_to_a_new_version_keeps_original()
    {
        await Seed();var input=WithRequirements();await Service.SaveJobMatrixAsync(input);var old=Assert.Single((await Service.LoadPayFrameworksAsync()).Matrices);
        Assert.True((await Service.CreatePositionMatrixAsync(new(old.Id,old.Titles[0].Id,2,new(2026,1,1),null))).Success);
        Assert.False((await Service.SaveJobMatrixAsync(input with{Id=old.Id,Profile=old.Profile})).Success);
        Assert.True((await Service.SaveJobMatrixAsync(WithRequirements("REQ-V2"))).Success);Assert.Equal(2,(await Service.LoadPayFrameworksAsync()).Matrices.Count);
    }
}
