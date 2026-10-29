using HRM.Application.Contracts.Pages;
using HRM.Application.Presentation;
using HRM.Migrations;
using Xunit;
namespace HRM.Api.UnitTests;
public sealed class SoftwareRequirementPresetTests
{
    [Theory][InlineData(10,3,48)][InlineData(9,3,0)][InlineData(8,2,0)][InlineData(7,1,0)]
    public void Source_rank_routes_and_notes_are_populated_without_invented_licences(int rank,int pathways,int firstMonths)
    {
        var source=SoftwareProfessionRequirements.SoftwareProfile(rank);Assert.Equal(pathways,source.QualificationPathways.Count);Assert.Equal(firstMonths,source.QualificationPathways[0].RelevantExperienceMonths);
        Assert.NotEmpty(source.LicensesAndTraining);Assert.NotEmpty(source.Qualifications);Assert.All(source.CredentialRequirements,x=>Assert.Equal("Conditional",x.Requirement));Assert.All(source.QualificationPathways,x=>{Assert.Equal(0,x.PostQualificationMonths);Assert.Equal(0,x.ManagerialExperienceMonths);Assert.Contains(SoftwareProfessionRequirements.Source,x.SourceReference);});
    }
    [Fact]public void Presets_fill_blank_notes_but_preserve_custom_entries_and_use_new_requirement_ids()
    {
        var source=SoftwareProfessionRequirements.SoftwareProfile(10);source.CredentialRequirements[0].Id=123;var profile=new JobMatrixProfile{RequirementsVersion="current",PublicServiceRank="10"};
        MatrixRequirementPresets.FillBlank(profile,source);Assert.NotEmpty(profile.LicensesAndTraining);Assert.NotEmpty(profile.Qualifications);Assert.Equal(0,profile.CredentialRequirements[0].Id);Assert.Equal("current",profile.RequirementsVersion);
        profile.LicensesAndTraining="HR custom note";profile.Qualifications="Existing qualification text";profile.QualificationPathways[0].Name="HR route";
        MatrixRequirementPresets.FillBlank(profile,SoftwareProfessionRequirements.SoftwareProfile(7));Assert.Equal("HR custom note",profile.LicensesAndTraining);Assert.Equal("Existing qualification text",profile.Qualifications);Assert.Equal("HR route",profile.QualificationPathways[0].Name);Assert.Equal(3,profile.QualificationPathways.Count);
    }
    [Fact]public void Changing_rank_refreshes_only_unchanged_autofilled_values()
    {
        var profile=new JobMatrixProfile();var before=MatrixRequirementPresets.Snapshot(profile);MatrixRequirementPresets.FillBlank(profile,SoftwareProfessionRequirements.SoftwareProfile(10));var after=MatrixRequirementPresets.Snapshot(profile);
        profile.LicensesAndTraining="HR custom licence note";
        MatrixRequirementPresets.RestoreUnmodified(profile,before,after);MatrixRequirementPresets.FillBlank(profile,SoftwareProfessionRequirements.SoftwareProfile(7));
        Assert.Single(profile.QualificationPathways);Assert.Equal(0,profile.QualificationPathways[0].RelevantExperienceMonths);Assert.Equal("HR custom licence note",profile.LicensesAndTraining);Assert.Contains("Level 7 or 8",profile.Qualifications);
    }
}
