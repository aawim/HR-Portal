namespace HRM.DTOs;
public sealed record WorkspaceSearchFilter(string SearchText, bool StaffOnly = false, bool RequireActivePosition = false, int? OrganisationId = null, IReadOnlyCollection<int>? AllowedStaffIds = null);
public sealed record WorkspaceSearchPage(List<WorkspaceSearchResult> Staff, List<WorkspaceSearchResult> Organisations);

public sealed record StaffWorkspaceIdentity(string Name, bool IsFormer, string? Position = null, string Status = "");
