using HRM.Application.Contracts.Pages;
namespace HRM.Application.Presentation;
public static class MatrixRequirementPresets
{
    public static JobMatrixProfile Snapshot(JobMatrixProfile value)=>System.Text.Json.JsonSerializer.Deserialize<JobMatrixProfile>(System.Text.Json.JsonSerializer.Serialize(value))!;
    public static void RestoreUnmodified(JobMatrixProfile target,JobMatrixProfile before,JobMatrixProfile after)
    {
        static bool Same<T>(T a,T b)=>System.Text.Json.JsonSerializer.Serialize(a)==System.Text.Json.JsonSerializer.Serialize(b);
        if(before.LicensesAndTraining!=after.LicensesAndTraining&&target.LicensesAndTraining==after.LicensesAndTraining)target.LicensesAndTraining=before.LicensesAndTraining;
        if(before.Qualifications!=after.Qualifications&&target.Qualifications==after.Qualifications)target.Qualifications=before.Qualifications;
        if(!Same(before.CredentialRequirements,after.CredentialRequirements)&&Same(target.CredentialRequirements,after.CredentialRequirements))target.CredentialRequirements=Snapshot(before).CredentialRequirements;
        if(!Same(before.QualificationPathways,after.QualificationPathways)&&Same(target.QualificationPathways,after.QualificationPathways))target.QualificationPathways=Snapshot(before).QualificationPathways;
    }
    public static void FillBlank(JobMatrixProfile target,JobMatrixProfile source)
    {
        if(string.IsNullOrWhiteSpace(target.LicensesAndTraining))target.LicensesAndTraining=source.LicensesAndTraining;
        if(string.IsNullOrWhiteSpace(target.Qualifications))target.Qualifications=source.Qualifications;
        if(target.CredentialRequirements.Count==0)target.CredentialRequirements=source.CredentialRequirements.Select(x=>x with{Id=0}).ToList();
        if(target.QualificationPathways.Count==0)target.QualificationPathways=source.QualificationPathways.Select(x=>x with{Id=0,Qualifications=x.Qualifications.Select(q=>q with{Id=0}).ToList()}).ToList();
    }
}
