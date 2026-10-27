using HRM.Services;
using HRM.Services.Contracts;
using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Organisation;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace HRM.UI.Services.Generated;
public sealed class OrganisationResourceSettingsServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.OrganisationResourceSettingsServiceContracts, HRM.Services.Contracts.OrganisationResourceSettingsServiceContracts.IOperations
{
public Task<OrganisationResourceData> LoadAsync(OrganisationResourceKind kind, int? organisationId = null) => api.InvokeAsync<OrganisationResourceData>(typeof(HRM.Services.Contracts.OrganisationResourceSettingsServiceContracts.IOperations),nameof(LoadAsync), [HRM.Application.Transport.ApiArgument.From(kind),HRM.Application.Transport.ApiArgument.From(organisationId)],default);
public Task<ServiceResult> SaveTypeAsync(OrganisationResourceKind kind, int id, string name, string? expectedName) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.OrganisationResourceSettingsServiceContracts.IOperations),nameof(SaveTypeAsync), [HRM.Application.Transport.ApiArgument.From(kind),HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(name),HRM.Application.Transport.ApiArgument.From(expectedName)],default);
public Task<ServiceResult> LinkCalendarAsync(int organisationId, int calendarId, DateTime from, DateTime? to) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.OrganisationResourceSettingsServiceContracts.IOperations),nameof(LinkCalendarAsync), [HRM.Application.Transport.ApiArgument.From(organisationId),HRM.Application.Transport.ApiArgument.From(calendarId),HRM.Application.Transport.ApiArgument.From(from),HRM.Application.Transport.ApiArgument.From(to)],default);
public Task<OrganisationDocumentTypesData> LoadDocumentTypesAsync() => api.InvokeAsync<OrganisationDocumentTypesData>(typeof(HRM.Services.Contracts.OrganisationResourceSettingsServiceContracts.IOperations),nameof(LoadDocumentTypesAsync), [],default);
public Task<ServiceResult> SaveDocumentTypeAsync(OrganisationDocumentType model, bool delete = false) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.OrganisationResourceSettingsServiceContracts.IOperations),nameof(SaveDocumentTypeAsync), [HRM.Application.Transport.ApiArgument.From(model),HRM.Application.Transport.ApiArgument.From(delete)],default);
}
