using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.ApprovalFlow;
using HRM.DTOs.UserContext;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
namespace HRM.Services.Contracts;
public abstract class StaffOnboardingServiceContracts
{
    public const string RequestTypeName = "Job Position Request";

    public const string PageUrl = "/admin/my/workspace/onboarding";

public interface IOperations
{
Task<StaffOnboardingData> LoadAsync(int? individualId = null)
;
Task<ServiceResult> SubmitAsync(StaffOnboardingForm model)
;
Task<List<StaffOnboardingRow>> GetRequestsAsync(int? requestId = null, bool pendingOnly = false)
;
Task<ServiceResult> ActAsync(int requestId, int expectedState, string action, string? comment)
;
}
}
