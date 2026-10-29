using HRM.DTOs;
namespace HRM.Application.Contracts.Pages;

public sealed record PayFrameworkInput(int Id,string Code,string Name,bool Active);
public sealed record MatrixStepInput(int Number,decimal JobAllowance,decimal? BasicSalaryOverride,bool Active=true);
public sealed record MatrixBenefitInput(string Name,decimal? Amount,string Frequency="Monthly");
public sealed record JobMatrixInput(int Id,int FrameworkId,string Code,string Name,int CurrencyId,decimal BasicSalary,
    DateTime From,DateTime? To,bool Active,List<string> Titles,List<MatrixStepInput> Steps,List<MatrixBenefitInput> Benefits)
{
    public JobMatrixProfile Profile {get;init;}=new();
}
public sealed record MatrixStepOption(int Id,int Number,decimal BasicSalary,decimal JobAllowance,bool Active);
public sealed record JobMatrixRow(int Id,int FrameworkId,string Code,string Name,int CurrencyId,decimal BasicSalary,
    DateTime From,DateTime? To,bool Active,bool InUse,List<PositionLookup> Titles,List<MatrixStepOption> Steps,List<MatrixBenefitInput> Benefits)
{
    public JobMatrixProfile Profile {get;init;}=new();
}
public sealed record JobMatrixProfile
{
    public int? JobFamilyId {get;set;}
    public int? ProfessionId {get;set;}
    public string Description {get;set;}="";
    public string PublicServiceRank {get;set;}="";
    public string InstitutionalCategory {get;set;}="";
    public string GradeCode {get;set;}="";
    public string JobFamily {get;set;}="";
    public string MisscoCode {get;set;}="";
    public byte? SkillLevel {get;set;}
    public decimal? AttendanceBenefitDaily {get;set;}
    public DateTime? RevisedDate {get;set;}
    public string LicensesAndTraining {get;set;}="";
    public string FieldsOfStudy {get;set;}="";
    public string ExperienceAreas {get;set;}="";
    public string Qualifications {get;set;}="";
    public string Duties {get;set;}="";
}
public sealed record PositionMatrixInput(int JobMatrixId,int TitleId,int Slots,DateTime From,DateTime? To);
public sealed record PositionPayInfo(string Currency,decimal BasicSalary,decimal JobAllowance,List<MatrixBenefitInput> Benefits)
{
    public List<MatrixStepOption> Steps {get;init;}=[];
    public int? SelectedStepId {get;init;}
}
public sealed record ProfessionOption(int Id,int FamilyId,string Name,string Tasks,List<string> Titles,List<string> MisscoCodes);
public sealed record PayFrameworkData(List<PositionFrameworkOption> Frameworks,List<JobMatrixRow> Matrices,
    List<PositionLookup> Currencies,bool CanManageFrameworks,bool CanManagePositions)
{
    public List<PositionLookup> JobFamilies {get;init;}=[];
    public List<ProfessionOption> Professions {get;init;}=[];
}
