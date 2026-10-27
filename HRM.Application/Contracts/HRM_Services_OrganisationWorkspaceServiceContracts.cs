using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Services.Interfaces;
using HRM.Enum;
namespace HRM.Services.Contracts;
public abstract class OrganisationWorkspaceServiceContracts
{

public interface IOperations
{
Task<OrganisationWorkspaceData> LoadAsync(int organisationId)
;
Task<(List<OrganisationWorkspaceStaff> Rows, int Total)> StaffAsync(int organisationId, string search, int departmentId, int page)
;
Task<(List<OrganisationWorkspaceDocument> Rows, int Total)> DocumentsAsync(int organisationId, int page)
;
Task<List<OrganisationWorkspaceResource>> ResourcesAsync(int organisationId, WorkspaceTab tab)
;
}
}
