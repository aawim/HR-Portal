using System.Data;
using System.Text.Json;
using HRM.DTOs;
using HRM.Enum;
using HRM.Services.Interfaces;
namespace HRM.Services.Contracts;
public abstract class WorkspaceRecentSearchServiceContracts
{
    public sealed record Entry(int OrganisationId, WorkspaceEntityType Type, int Id, DateTime AtUtc);

public interface IOperations
{
Task<WorkspaceSearchResult?> ResolveAsync(WorkspaceEntityType type, int id)
;
Task<List<WorkspaceSearchResult>> GetAsync()
;
Task<WorkspaceSearchResult?> RememberAsync(WorkspaceSearchResult selected)
;
}
}
