using HRM.Services;
using HRM.Services.Contracts;
using HRM.Models;
using HRM.Services.Interfaces;
using System.Globalization;
namespace HRM.UI.Services.Generated;
public sealed class FormerStaffServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.FormerStaffServiceContracts, HRM.Services.Contracts.FormerStaffServiceContracts.IOperations
{
public Task<FormerStaffData> SearchAsync(string? search=null,CancellationToken ct=default,FormerStaffFilters? filters=null) => api.InvokeAsync<FormerStaffData>(typeof(HRM.Services.Contracts.FormerStaffServiceContracts.IOperations),nameof(SearchAsync), [HRM.Application.Transport.ApiArgument.From(search),HRM.Application.Transport.ApiArgument.From(filters)],ct);
public Task<global::HRM.DTOs.Contracts.ExportReportDto> DetailAsync(int person,CancellationToken ct=default) => api.InvokeAsync<global::HRM.DTOs.Contracts.ExportReportDto>(typeof(HRM.Services.Contracts.FormerStaffServiceContracts.IOperations),nameof(DetailAsync), [HRM.Application.Transport.ApiArgument.From(person)],ct);
}
