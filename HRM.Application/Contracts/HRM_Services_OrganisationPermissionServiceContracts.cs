using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Organisation;
using HRM.DTOs.Roles;
using HRM.DTOs.UserContext;
using HRM.Services.Interfaces;
namespace HRM.Services.Contracts;
public abstract class OrganisationPermissionServiceContracts
{

public interface IOperations
{
Task<OrganisationPermissionData> LoadAsync(int? organisationId = null)
;
Task<ServiceResult> SaveAsync(int organisationId, IEnumerable<int> selectedIds, IEnumerable<int> expectedIds, string reason)
;
}
}
