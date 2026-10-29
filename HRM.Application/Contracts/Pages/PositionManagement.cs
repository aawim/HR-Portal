using HRM.DTOs;
namespace HRM.Application.Contracts.Pages;
public sealed record PositionFrameworkOption(int Id,string Code,string Name,bool Active);
public sealed record PositionMatrixOption(int Id,string Name,decimal BasicSalary,int ApprovedSlots)
{
    public int FrameworkId {get;init;}
}
public sealed record PositionLookup(int Id,string Name);
public sealed record PositionSalaryRow(decimal Amount,DateOnly EffectiveDate);
public sealed record ManagedPosition(int Id,string Name,string? DhivehiName,string? Description,bool Active,bool Global,int? FrameworkId,int? MatrixId,string Framework,int CurrentStaff,bool CanEdit)
{
    public int? TypeId {get;init;}
    public int? RankId {get;init;}
    public int? ClassificationId {get;init;}
    public List<PositionSalaryRow> Salaries {get;init;}=[];
}
public sealed record PositionManagementData(List<PositionFrameworkOption> Frameworks,List<PositionMatrixOption> Matrices,List<ManagedPosition> Positions,bool CanManage,bool IsSuperAdministrator)
{
    public List<PositionLookup> Types {get;init;}=[];
    public List<PositionLookup> Ranks {get;init;}=[];
    public List<PositionLookup> Classifications {get;init;}=[];
}
public sealed record PositionInput(int Id,string Name,string? DhivehiName,string? Description,bool Active,bool Global,int FrameworkId,int? MatrixId)
{
    public int? TypeId {get;init;}
    public int? RankId {get;init;}
    public int? ClassificationId {get;init;}
    public decimal? BasicSalary {get;init;}
    public DateOnly? SalaryEffectiveDate {get;init;}
}
public interface IPositionManagement
{
    Task<PositionManagementData> LoadAsync();
    Task<ServiceResult> SaveAsync(PositionInput input);
    Task<PayFrameworkData> LoadPayFrameworksAsync();
    Task<ServiceResult> SaveFrameworkAsync(PayFrameworkInput input);
    Task<ServiceResult> SaveJobMatrixAsync(JobMatrixInput input);
    Task<ServiceResult> CreatePositionMatrixAsync(PositionMatrixInput input);
}
