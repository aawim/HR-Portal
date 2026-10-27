using HRM.Services;
using HRM.Services.Contracts;
using HRM.Models;
using HRM.DTOs;
using HRM.Services.Interfaces;
using HRM.DTOs.Attendance;
using System.Threading;
namespace HRM.UI.Services.Generated;
public sealed class AttendanceLogServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.AttendanceLogServiceContracts, HRM.Services.Contracts.AttendanceLogServiceContracts.IOperations
{
public Task<List<TeamAttendanceDto>> GetMyTeamAttendanceTodayAsync() => api.InvokeAsync<List<TeamAttendanceDto>>(typeof(HRM.Services.Contracts.AttendanceLogServiceContracts.IOperations),nameof(GetMyTeamAttendanceTodayAsync), [],default);
public Task<bool> CanUseTestClockAsync() => api.InvokeAsync<bool>(typeof(HRM.Services.Contracts.AttendanceLogServiceContracts.IOperations),nameof(CanUseTestClockAsync), [],default);
public Task AddClockEventAsync(
            int individualId,
            DateTime? checkTime = null) => api.InvokeAsync(typeof(HRM.Services.Contracts.AttendanceLogServiceContracts.IOperations),nameof(AddClockEventAsync), [HRM.Application.Transport.ApiArgument.From(individualId),HRM.Application.Transport.ApiArgument.From(checkTime)],default);
public Task AddAttendanceCheckAsync(int individualId, int mode) => api.InvokeAsync(typeof(HRM.Services.Contracts.AttendanceLogServiceContracts.IOperations),nameof(AddAttendanceCheckAsync), [HRM.Application.Transport.ApiArgument.From(individualId),HRM.Application.Transport.ApiArgument.From(mode)],default);
public Task<List<global::HRM.DTOs.Contracts.AttendanceLogDto>> GetAttendanceLogsAsync(int userId, DateTime startDate, DateTime endDate) => api.InvokeAsync<List<global::HRM.DTOs.Contracts.AttendanceLogDto>>(typeof(HRM.Services.Contracts.AttendanceLogServiceContracts.IOperations),nameof(GetAttendanceLogsAsync), [HRM.Application.Transport.ApiArgument.From(userId),HRM.Application.Transport.ApiArgument.From(startDate),HRM.Application.Transport.ApiArgument.From(endDate)],default);
public Task<List<global::HRM.DTOs.Leave.JobLeaveTypeDto>> GetUserLeaveBalancesAsync(string identifier) => api.InvokeAsync<List<global::HRM.DTOs.Leave.JobLeaveTypeDto>>(typeof(HRM.Services.Contracts.AttendanceLogServiceContracts.IOperations),nameof(GetUserLeaveBalancesAsync), [HRM.Application.Transport.ApiArgument.From(identifier)],default);
public Task<List<global::HRM.DTOs.Contracts.AttendanceLogDto>> GetUnevenLogsAsync(int individualId) => api.InvokeAsync<List<global::HRM.DTOs.Contracts.AttendanceLogDto>>(typeof(HRM.Services.Contracts.AttendanceLogServiceContracts.IOperations),nameof(GetUnevenLogsAsync), [HRM.Application.Transport.ApiArgument.From(individualId)],default);
public Task<List<global::HRM.DTOs.Contracts.LeafDto>> GetLeaveRequestsAsync(int jobId) => api.InvokeAsync<List<global::HRM.DTOs.Contracts.LeafDto>>(typeof(HRM.Services.Contracts.AttendanceLogServiceContracts.IOperations),nameof(GetLeaveRequestsAsync), [HRM.Application.Transport.ApiArgument.From(jobId)],default);
}
