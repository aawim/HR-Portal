using HRM.Services;
using HRM.Services.Contracts;
using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Attendance;
using HRM.DTOs.ApprovalFlow;
using HRM.Models;
using HRM.Models.Workflow;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using HRM.Models.WorkPlanning;
namespace HRM.UI.Services.Generated;
public sealed class LateCheckInRequestServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.LateCheckInRequestServiceContracts, HRM.Services.Contracts.LateCheckInRequestServiceContracts.IOperations
{
public Task<ServiceResult> SubmitForDateAsync(DateTime date, TimeOnly time, string reason, string kind) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.LateCheckInRequestServiceContracts.IOperations),nameof(SubmitForDateAsync), [HRM.Application.Transport.ApiArgument.From(date),HRM.Application.Transport.ApiArgument.From(time),HRM.Application.Transport.ApiArgument.From(reason),HRM.Application.Transport.ApiArgument.From(kind)],default);
public Task<List<StaffScheduleAttendanceDto>> GetCandidatesAsync(DateTime from, DateTime to, string kind = "LateCheckIn") => api.InvokeAsync<List<StaffScheduleAttendanceDto>>(typeof(HRM.Services.Contracts.LateCheckInRequestServiceContracts.IOperations),nameof(GetCandidatesAsync), [HRM.Application.Transport.ApiArgument.From(from),HRM.Application.Transport.ApiArgument.From(to),HRM.Application.Transport.ApiArgument.From(kind)],default);
public Task<ServiceResult> SubmitAsync(long assignment, DateTime requestedTime, string reason, string kind = "LateCheckIn") => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.LateCheckInRequestServiceContracts.IOperations),nameof(SubmitAsync), [HRM.Application.Transport.ApiArgument.From(assignment),HRM.Application.Transport.ApiArgument.From(requestedTime),HRM.Application.Transport.ApiArgument.From(reason),HRM.Application.Transport.ApiArgument.From(kind)],default);
public Task<List<LateCheckInRow>> LoadAsync(DateTime from, DateTime to, string? kind = null, bool pendingOnly = false) => api.InvokeAsync<List<LateCheckInRow>>(typeof(HRM.Services.Contracts.LateCheckInRequestServiceContracts.IOperations),nameof(LoadAsync), [HRM.Application.Transport.ApiArgument.From(from),HRM.Application.Transport.ApiArgument.From(to),HRM.Application.Transport.ApiArgument.From(kind),HRM.Application.Transport.ApiArgument.From(pendingOnly)],default);
public Task<ServiceResult> ActAsync(int id, int expectedState, string action, string? comment) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.LateCheckInRequestServiceContracts.IOperations),nameof(ActAsync), [HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(expectedState),HRM.Application.Transport.ApiArgument.From(action),HRM.Application.Transport.ApiArgument.From(comment)],default);
}
