using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
namespace HRM.Services.Contracts;
public abstract class WeeklyHourExceptionServiceContracts
{
    public const string TypeName = "Weekly Hours Exception Request";

    public const string PageUrl = "/online/attendance/attendance-request/weekly-hours-exception";

public interface IOperations
{
Task<ServiceResult> SubmitAsync(DateTime day, int minutes, string reason)
;
Task<List<WeeklyHourExceptionRow>> LoadAsync(bool pendingOnly)
;
Task<ServiceResult> ActAsync(int id, int expectedState, string action, string? comment)
;
}
}
