using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Organisation;
using HRM.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace HRM.Services.Contracts;
public abstract class OrganisationResourceSettingsServiceContracts
{

public interface IOperations
{
Task<OrganisationResourceData> LoadAsync(OrganisationResourceKind kind, int? organisationId = null)
;
Task<ServiceResult> SaveTypeAsync(OrganisationResourceKind kind, int id, string name, string? expectedName)
;
Task<ServiceResult> LinkCalendarAsync(int organisationId, int calendarId, DateTime from, DateTime? to)
;
Task<OrganisationDocumentTypesData> LoadDocumentTypesAsync()
;
Task<ServiceResult> SaveDocumentTypeAsync(OrganisationDocumentType model, bool delete = false)
;
}
}
