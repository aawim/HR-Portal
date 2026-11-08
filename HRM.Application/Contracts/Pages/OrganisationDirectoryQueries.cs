using HRM.DTOs;
namespace HRM.Application.Contracts.Pages;

public enum OrganisationUnitKind { Team = 1, Group = 2, Structure = 3 }
public sealed record OrganisationUnitKey(OrganisationUnitKind Kind, int Id);
public sealed record DirectoryOption(int Id, string Name);
public sealed record OrganisationUnitSummary(int Id, OrganisationUnitKind Kind, int OrganisationId, string Name,
    string? NameDhivehi, int? TypeId, DateTime? StartsOn, DateTime? EndsOn, bool IsActive, int Version);
public sealed record OrganisationMember(int StaffId, string Name, string? EmployeeNumber, DateTime StartsOn, DateTime? EndsOn, bool IsSupervisor);
public sealed record OrganisationMembershipPeriod(DateOnly Month, int Joined, int Left, int ActiveStaff);
public sealed record OrganisationUnitSchedule(int AssignmentId, int TemplateId, string Name, string Purpose,
    DateOnly StartsOn, DateOnly? EndsOn, bool IsActive, int Version);
public sealed record OrganisationUnitLeaveSet(int AssignmentId, int LeaveSetId, string Name, bool IsPublic, bool IsGlobal,
    bool IsValid, List<string> LeaveTypes);
public sealed record OrganisationUnitDetails(OrganisationUnitSummary Unit, List<OrganisationMember> CurrentStaff,
    List<OrganisationMember> FormerStaff, List<OrganisationMembershipPeriod> History, List<OrganisationUnitSchedule> Schedules,
    List<OrganisationUnitLeaveSet> LeaveSets, List<DirectoryOption> AvailableLeaveSets, bool CanManage);

/// <summary>Tenant-bound read models for existing organisation panels. Mutations use the canonical settings services.</summary>
public interface IOrganisationDirectoryQueries
{
    Task<List<OrgNodeDto>> GetApprovedStructureAsync(int? organisationId = null, CancellationToken ct = default);
    Task<List<OrganisationUnitSummary>> GetTeamsAsync(int? organisationId = null, CancellationToken ct = default);
    Task<OrganisationUnitDetails?> GetDetailsAsync(OrganisationUnitKey unit, CancellationToken ct = default);
}
