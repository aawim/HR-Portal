using System.Data;
using System.Text.Json;
using HRM.DTOs;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using HRM.Components.Shared;
namespace HRM.Services.Contracts;
public abstract class OvertimeRequestServiceContracts
{
    public const string TypeName="Overtime Claim Request";
public const string PageUrl="/online/overtime-requests";

    public const string PreapprovalTypeName = "Overtime Preapproval Request";

    public const string ClockTypeName = "Overtime Clock Event Request";

public interface IOperations
{
Task<ServiceResult> SubmitAsync(DateTime date, int minutes, string reason) ;
Task<List<OvertimeRow>> LoadAsync(bool pendingOnly=false)
;
Task<ServiceResult> ActAsync(int id,int expectedState,string action,string? comment)
;
Task<ServiceResult> RecordPaymentAsync(int id,string reference)
;
Task<ServiceResult> ProcessPayrollAsync(IEnumerable<int> requestIds,DateTime month,string reference,Guid clientBatchId)
;
Task<ServiceResult> ResubmitClaimAsync(int id,string reason)
;
Task<List<OvertimeClockOption>> LoadClockEventsAsync(DateTime date)
;
Task<List<OvertimeTaskOption>> LoadApprovedTasksAsync()
;
Task<ServiceResult> SubmitClaimAsync(int startLogId, int endLogId, string reason, int? taskId = null)
;
Task<ServiceResult> SubmitWorkAsync(string kind, DateTime from, DateTime to, string reason, List<string> tasks,
        DateTime? proposedStart = null, DateTime? proposedEnd = null)
;
Task<List<OvertimeWorkRow>> LoadWorkAsync(bool pendingOnly = false)
;
Task<ServiceResult> ActWorkAsync(int id, int expectedState, string action, string? comment)
;
}
}
