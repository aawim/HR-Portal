using HRM.Services;
using HRM.Services.Contracts;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.Enum;
namespace HRM.UI.Services.Generated;
public sealed class OrganisationWorkspaceServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.OrganisationWorkspaceServiceContracts, HRM.Services.Contracts.OrganisationWorkspaceServiceContracts.IOperations
{
public Task<OrganisationWorkspaceData> LoadAsync(int organisationId) => api.InvokeAsync<OrganisationWorkspaceData>(typeof(HRM.Services.Contracts.OrganisationWorkspaceServiceContracts.IOperations),nameof(LoadAsync), [HRM.Application.Transport.ApiArgument.From(organisationId)],default);
public Task<(List<OrganisationWorkspaceStaff> Rows, int Total)> StaffAsync(int organisationId, string search, int departmentId, int page) => api.InvokeAsync<(List<OrganisationWorkspaceStaff> Rows, int Total)>(typeof(HRM.Services.Contracts.OrganisationWorkspaceServiceContracts.IOperations),nameof(StaffAsync), [HRM.Application.Transport.ApiArgument.From(organisationId),HRM.Application.Transport.ApiArgument.From(search),HRM.Application.Transport.ApiArgument.From(departmentId),HRM.Application.Transport.ApiArgument.From(page)],default);
public Task<(List<OrganisationWorkspaceDocument> Rows, int Total)> DocumentsAsync(int organisationId, int page) => api.InvokeAsync<(List<OrganisationWorkspaceDocument> Rows, int Total)>(typeof(HRM.Services.Contracts.OrganisationWorkspaceServiceContracts.IOperations),nameof(DocumentsAsync), [HRM.Application.Transport.ApiArgument.From(organisationId),HRM.Application.Transport.ApiArgument.From(page)],default);
public Task<List<OrganisationWorkspaceResource>> ResourcesAsync(int organisationId, WorkspaceTab tab) => api.InvokeAsync<List<OrganisationWorkspaceResource>>(typeof(HRM.Services.Contracts.OrganisationWorkspaceServiceContracts.IOperations),nameof(ResourcesAsync), [HRM.Application.Transport.ApiArgument.From(organisationId),HRM.Application.Transport.ApiArgument.From(tab)],default);
}
