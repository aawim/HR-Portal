using HRM.Services;
using HRM.Services.Contracts;
using System.ComponentModel.DataAnnotations;
using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Organisation;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class OrganisationSettingsServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.OrganisationSettingsServiceContracts, HRM.Services.Contracts.OrganisationSettingsServiceContracts.IOperations
{
public Task<OrganisationSettingsData> LoadAsync() => api.InvokeAsync<OrganisationSettingsData>(typeof(HRM.Services.Contracts.OrganisationSettingsServiceContracts.IOperations),nameof(LoadAsync), [],default);
public Task<OrganisationLinksDto> GetLinksAsync(int id) => api.InvokeAsync<OrganisationLinksDto>(typeof(HRM.Services.Contracts.OrganisationSettingsServiceContracts.IOperations),nameof(GetLinksAsync), [HRM.Application.Transport.ApiArgument.From(id)],default);
public Task<ServiceResult> SaveDetailsAsync(OrganisationSettingsDto model) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.OrganisationSettingsServiceContracts.IOperations),nameof(SaveDetailsAsync), [HRM.Application.Transport.ApiArgument.From(model)],default);
public Task<List<OrganisationSettingsOption>> SearchAddressesAsync(int organisationId, string query) => api.InvokeAsync<List<OrganisationSettingsOption>>(typeof(HRM.Services.Contracts.OrganisationSettingsServiceContracts.IOperations),nameof(SearchAddressesAsync), [HRM.Application.Transport.ApiArgument.From(organisationId),HRM.Application.Transport.ApiArgument.From(query)],default);
public Task<ServiceResult> SaveAddressAsync(int id, OrganisationAddressDto model) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.OrganisationSettingsServiceContracts.IOperations),nameof(SaveAddressAsync), [HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(model)],default);
public Task<OrganisationAddressDto> GetAddressAsync(int id, int linkId) => api.InvokeAsync<OrganisationAddressDto>(typeof(HRM.Services.Contracts.OrganisationSettingsServiceContracts.IOperations),nameof(GetAddressAsync), [HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(linkId)],default);
public Task<ServiceResult> DeleteAddressAsync(int id, int linkId) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.OrganisationSettingsServiceContracts.IOperations),nameof(DeleteAddressAsync), [HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(linkId)],default);
public Task<ServiceResult> SaveContactAsync(int id, OrganisationContactDto model) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.OrganisationSettingsServiceContracts.IOperations),nameof(SaveContactAsync), [HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(model)],default);
public Task<List<OrganisationStaffRow>> GetStaffAsync(int id) => api.InvokeAsync<List<OrganisationStaffRow>>(typeof(HRM.Services.Contracts.OrganisationSettingsServiceContracts.IOperations),nameof(GetStaffAsync), [HRM.Application.Transport.ApiArgument.From(id)],default);
public Task<ServiceResult> DeleteContactAsync(int id, int contactId) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.OrganisationSettingsServiceContracts.IOperations),nameof(DeleteContactAsync), [HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(contactId)],default);
public Task<ServiceResult> DeleteOrganisationAsync(int id) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.OrganisationSettingsServiceContracts.IOperations),nameof(DeleteOrganisationAsync), [HRM.Application.Transport.ApiArgument.From(id)],default);
public Task<List<OrganisationSettingsOption>> GetTeamOrganisationsAsync() => api.InvokeAsync<List<OrganisationSettingsOption>>(typeof(HRM.Services.Contracts.OrganisationSettingsServiceContracts.IOperations),nameof(GetTeamOrganisationsAsync), [],default);
public Task<OrganisationTeamSettingsData> GetTeamSettingsAsync(int organisationId) => api.InvokeAsync<OrganisationTeamSettingsData>(typeof(HRM.Services.Contracts.OrganisationSettingsServiceContracts.IOperations),nameof(GetTeamSettingsAsync), [HRM.Application.Transport.ApiArgument.From(organisationId)],default);
public Task<ServiceResult> SetTeamSupervisorAsync(int organisationId, string kind, int teamId, int staffId, bool assign) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.OrganisationSettingsServiceContracts.IOperations),nameof(SetTeamSupervisorAsync), [HRM.Application.Transport.ApiArgument.From(organisationId),HRM.Application.Transport.ApiArgument.From(kind),HRM.Application.Transport.ApiArgument.From(teamId),HRM.Application.Transport.ApiArgument.From(staffId),HRM.Application.Transport.ApiArgument.From(assign)],default);
}
