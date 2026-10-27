using HRM.Services;
using HRM.Services.Contracts;
using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Organisation;
using HRM.DTOs.Roles;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class OrganisationPermissionServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.OrganisationPermissionServiceContracts, HRM.Services.Contracts.OrganisationPermissionServiceContracts.IOperations
{
public Task<OrganisationPermissionData> LoadAsync(int? organisationId = null) => api.InvokeAsync<OrganisationPermissionData>(typeof(HRM.Services.Contracts.OrganisationPermissionServiceContracts.IOperations),nameof(LoadAsync), [HRM.Application.Transport.ApiArgument.From(organisationId)],default);
public Task<ServiceResult> SaveAsync(int organisationId, IEnumerable<int> selectedIds, IEnumerable<int> expectedIds, string reason) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.OrganisationPermissionServiceContracts.IOperations),nameof(SaveAsync), [HRM.Application.Transport.ApiArgument.From(organisationId),HRM.Application.Transport.ApiArgument.From(selectedIds),HRM.Application.Transport.ApiArgument.From(expectedIds),HRM.Application.Transport.ApiArgument.From(reason)],default);
}
