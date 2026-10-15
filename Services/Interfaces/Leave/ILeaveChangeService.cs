using HRM.DTOs;
using HRM.DTOs.Leave;

namespace HRM.Services.Interfaces.Leave;

public interface ILeaveChangeService
{
    Task<List<LeaveChangeLeaveDto>> GetMyLeavesAsync();
    Task<List<LeaveChangeRequestDto>> GetMyRequestsAsync();
    Task<List<LeaveChangeRequestDto>> GetAssignedRequestsAsync();
    Task<ServiceResult> SubmitAsync(LeaveChangeApplicationDto model);
    Task<ServiceResult> ExecuteActionAsync(int requestId, RequestActionSubmissionDto action);
    Task<List<PendingLeaveRequestDto>> GetPendingLeaveRequestsAsync();
    Task<ServiceResult> ExecuteLeaveActionAsync(int requestId, RequestActionSubmissionDto action);
}
