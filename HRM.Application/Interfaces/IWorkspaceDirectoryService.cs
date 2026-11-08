using HRM.DTOs;
namespace HRM.Services.Interfaces;
public interface IWorkspaceDirectoryService
{
    Task<StaffWorkspaceIdentity?> GetStaffIdentityAsync(int staffId, CancellationToken ct = default);
    Task<WorkspaceSearchPage> SearchAsync(WorkspaceSearchFilter filter, CancellationToken ct = default);
}
