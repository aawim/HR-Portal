using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Attendance;
using HRM.DTOs.ApprovalFlow;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
namespace HRM.Services.Contracts;
public abstract class LateCheckInRequestServiceContracts
{
    public const string TypeName = "Late Check In Request";

    public const string PageUrl = "/online/attendance/attendance-request/late-check-in";

    public const string EarlyTypeName = "Early Check Out Request";

    public const string EarlyPageUrl = "/online/attendance/attendance-request/early-check-out";

public interface IOperations
{
Task<ServiceResult> SubmitForDateAsync(DateTime date, TimeOnly time, string reason, string kind)
;
Task<List<StaffScheduleAttendanceDto>> GetCandidatesAsync(DateTime from, DateTime to, string kind = "LateCheckIn")
;
Task<ServiceResult> SubmitAsync(long assignment, DateTime requestedTime, string reason, string kind = "LateCheckIn")
;
Task<List<LateCheckInRow>> LoadAsync(DateTime from, DateTime to, string? kind = null, bool pendingOnly = false)
;
Task<ServiceResult> ActAsync(int id, int expectedState, string action, string? comment)
;
}
}
