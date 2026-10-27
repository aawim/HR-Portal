using System.ComponentModel.DataAnnotations;
using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Organisation;
using HRM.DTOs.UserContext;
using HRM.Services.Interfaces;
namespace HRM.Services.Contracts;
public abstract class OrganisationSettingsServiceContracts
{

public interface IOperations
{
Task<OrganisationSettingsData> LoadAsync()
;
Task<OrganisationLinksDto> GetLinksAsync(int id)
;
Task<ServiceResult> SaveDetailsAsync(OrganisationSettingsDto model)
;
Task<List<OrganisationSettingsOption>> SearchAddressesAsync(int organisationId, string query)
;
Task<ServiceResult> SaveAddressAsync(int id, OrganisationAddressDto model)
;
Task<OrganisationAddressDto> GetAddressAsync(int id, int linkId)
;
Task<ServiceResult> DeleteAddressAsync(int id, int linkId)
;
Task<ServiceResult> SaveContactAsync(int id, OrganisationContactDto model)
;
Task<List<OrganisationStaffRow>> GetStaffAsync(int id)
;
Task<ServiceResult> DeleteContactAsync(int id, int contactId)
;
Task<ServiceResult> DeleteOrganisationAsync(int id)
;
Task<List<OrganisationSettingsOption>> GetTeamOrganisationsAsync()
;
Task<OrganisationTeamSettingsData> GetTeamSettingsAsync(int organisationId)
;
Task<ServiceResult> SetTeamSupervisorAsync(int organisationId, string kind, int teamId, int staffId, bool assign)
;
}
}
