using HRM.DTOs;
namespace HRM.Application.Contracts.Pages;
public sealed record PositionFrameworkOption(int Id,string Code,string Name,bool Active);
public sealed record PositionMatrixOption(int Id,string Name,decimal BasicSalary,int ApprovedSlots);
public sealed record ManagedPosition(int Id,string Name,string? DhivehiName,string? Description,bool Active,bool Global,int? FrameworkId,int? MatrixId,string Framework,int CurrentStaff,bool CanEdit);
public sealed record PositionManagementData(List<PositionFrameworkOption> Frameworks,List<PositionMatrixOption> Matrices,List<ManagedPosition> Positions,bool CanManage,bool IsSuperAdministrator);
public sealed record PositionInput(int Id,string Name,string? DhivehiName,string? Description,bool Active,bool Global,int FrameworkId,int? MatrixId);
public interface IPositionManagement
{
    Task<PositionManagementData> LoadAsync();
    Task<ServiceResult> SaveAsync(PositionInput input);
}
