using HRM.Services.Leave;
using HRM.Services.Leave.Contracts;
using System.Data;
using HRM.Components.Shared;
using HRM.DTOs.Leave;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class LeaveCalendarServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Leave.Contracts.LeaveCalendarServiceContracts, HRM.Services.Leave.Contracts.LeaveCalendarServiceContracts.IOperations
{
public Task<List<LeaveCalendarEntry>> LoadAsync(DateTime month, bool includeTeam, int? applicantJobId = null) => api.InvokeAsync<List<LeaveCalendarEntry>>(typeof(HRM.Services.Leave.Contracts.LeaveCalendarServiceContracts.IOperations),nameof(LoadAsync), [HRM.Application.Transport.ApiArgument.From(month),HRM.Application.Transport.ApiArgument.From(includeTeam),HRM.Application.Transport.ApiArgument.From(applicantJobId)],default);
public Task SavePlanAsync(DateTime from, DateTime to, string title) => api.InvokeAsync(typeof(HRM.Services.Leave.Contracts.LeaveCalendarServiceContracts.IOperations),nameof(SavePlanAsync), [HRM.Application.Transport.ApiArgument.From(from),HRM.Application.Transport.ApiArgument.From(to),HRM.Application.Transport.ApiArgument.From(title)],default);
public Task RemovePlanAsync(int id) => api.InvokeAsync(typeof(HRM.Services.Leave.Contracts.LeaveCalendarServiceContracts.IOperations),nameof(RemovePlanAsync), [HRM.Application.Transport.ApiArgument.From(id)],default);
}
