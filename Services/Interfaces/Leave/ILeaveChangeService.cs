using HRM.DTOs;
using HRM.DTOs.Leave;

namespace HRM.Services.Interfaces.Leave;

public interface ILeaveChangeService
{
    Task<List<MyLeaveApplicationDto>> GetMyApplicationsAsync();
    Task<MyLeaveApplicationDto> GetMyApplicationDetailsAsync(int requestId);
    Task<ReturnedLeaveApplicationDto> GetReturnedApplicationAsync(int requestId);
    Task<ServiceResult> ResubmitApplicationAsync(ReturnedLeaveApplicationDto model);
    Task<List<LeaveChangeLeaveDto>> GetMyLeavesAsync();
    Task<List<LeaveChangeRequestDto>> GetMyRequestsAsync();
    Task<List<LeaveChangeRequestDto>> GetAssignedRequestsAsync();
    Task<ServiceResult> SubmitAsync(LeaveChangeApplicationDto model);
    Task<ServiceResult> ExecuteActionAsync(int requestId, RequestActionSubmissionDto action);
    Task<List<PendingLeaveRequestDto>> GetPendingLeaveRequestsAsync();
    Task<ServiceResult> ExecuteLeaveActionAsync(int requestId, RequestActionSubmissionDto action);
}
