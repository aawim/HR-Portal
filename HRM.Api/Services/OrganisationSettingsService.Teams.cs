using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Organisation;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public partial class OrganisationSettingsService
{
    private static readonly string[] TeamViewKeys =
    [
        "Organisation.Structure.View", "Organisation.Structure.Supervisor.Add", "Organisation.Structure.Supervisor.Edit",
        "UserOrganisations.Admin.View", "Organisation.Teams.View", "Organisation.Teams.Edit", "Organisation.Edit", "UserOrganisations.View", "UserOrganisations.Edit"
    ];

    private async Task<bool> HasTeamPermissionAsync(params string[] keys)
    {
        var user = await access.RequireContextAsync();
        if (user.IsSuperAdministrator || await access.HasPermissionAsync("Admin")) return true;
        foreach (var key in keys) if (await access.HasPermissionAsync(key)) return true;
        return false;
    }

    public async Task<List<OrganisationSettingsOption>> GetTeamOrganisationsAsync()
    {
        await RequireAsync(0, TeamViewKeys);
        var user = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();
        var current = user.IsSuperAdministrator ? 0 : await access.GetCurrentOrganisationIdAsync();
        return await db.Organisations.AsNoTracking().Where(o => user.IsSuperAdministrator || o.BusinessEntityID == current)
            .OrderBy(x => x.OrganisationName).Select(x => new OrganisationSettingsOption(x.BusinessEntityID, x.OrganisationName)).ToListAsync();
    }

    public async Task<OrganisationTeamSettingsData> GetTeamSettingsAsync(int organisationId)
    {
        if (organisationId <= 0) throw new InvalidOperationException("Select an organisation.");
        await RequireAsync(organisationId, TeamViewKeys);
        await using var db = await factory.CreateDbContextAsync();
        var today = DateTime.UtcNow.AddHours(5).Date;
        var departments = await db.OrganisationStructures.AsNoTracking()
            .Where(x => x.OrganisationBusinessEntityID == organisationId && x.OrganisationStructureStateId == 3)
            .OrderBy(x => x.Name).Select(x => new OrganisationTeamSettingsRow { Id = x.OrganisationStructureId, Kind = "Department", Name = x.Name,
                StructureTypeId = x.OrganisationStructureTypeId }).ToListAsync();
        var structureTypeIds = departments.Select(x => x.StructureTypeId).ToList();
        var structureTypes = await db.OrganisationStructureTypes.AsNoTracking()
            .Where(x => structureTypeIds.Contains(x.OrganisationStructureTypeId)).OrderBy(x => x.TypeName)
            .Select(x => new OrganisationSettingsOption(x.OrganisationStructureTypeId, x.TypeName)).ToListAsync();
        var teams = await db.Teams.AsNoTracking().Where(x => x.OrganisationId == organisationId && x.IsValid == true &&
            (x.StartDate == null || x.StartDate <= today) && (x.EndDate == null || x.EndDate >= today))
            .OrderBy(x => x.Name).Select(x => new OrganisationTeamSettingsRow { Id = x.TeamId, Kind = "Team", Name = x.Name }).ToListAsync();
        var departmentAssignments = await db.OrganisationStructureSupervisors.AsNoTracking()
            .Where(x => x.OrganisationStructure.OrganisationBusinessEntityID == organisationId && x.IsActive && x.IsValid)
            .Select(x => new { Id = x.OrganisationStructureId, Person = x.SupervisorStaffId,
                Name = x.SupervisorStaff.Individual.FirstNameEnglish + " " + (x.SupervisorStaff.Individual.MiddleNameEnglish ?? "") + " " + (x.SupervisorStaff.Individual.LastNameEnglish ?? "") }).ToListAsync();
        var teamAssignments = await db.TeamStaffs.AsNoTracking().Where(x => x.Team.OrganisationId == organisationId &&
            x.IsValid && x.IsSuperVisor && x.StartDate <= today && (x.EndDate == null || x.EndDate >= today))
            .Select(x => new { Id = x.TeamId, Person = x.StaffId,
                Name = x.Staff.Individual.FirstNameEnglish + " " + (x.Staff.Individual.MiddleNameEnglish ?? "") + " " + (x.Staff.Individual.LastNameEnglish ?? "") }).ToListAsync();
        foreach (var row in departments)
            row.Supervisors = departmentAssignments.Where(x => x.Id == row.Id).DistinctBy(x => x.Person).Select(x => new OrganisationSettingsOption(x.Person, x.Name.Trim())).ToList();
        foreach (var row in teams)
            row.Supervisors = teamAssignments.Where(x => x.Id == row.Id).DistinctBy(x => x.Person).Select(x => new OrganisationSettingsOption(x.Person, x.Name.Trim())).ToList();
        var activeJobs = db.Jobs.AsNoTracking().Where(j => j.OrganisationID == organisationId && j.JobStateId == SharedConfig.JobStates.APPROVED &&
            j.JoinedDate <= today && (!j.TerminatedDate.HasValue || j.TerminatedDate > today));
        var departmentIds = departments.Select(d => d.Id).ToList();
        var departmentMembers = await activeJobs.Where(j => departmentIds.Contains(j.OrganisationStructureId))
            .Select(j => new { TargetId=j.OrganisationStructureId, Person=j.IndividualID,
                Name=(j.Individual.FirstNameEnglish ?? "")+" "+(j.Individual.MiddleNameEnglish ?? "")+" "+(j.Individual.LastNameEnglish ?? "") }).ToListAsync();
        var teamIds = teams.Select(t => t.Id).ToList();
        var teamMembers = await db.TeamStaffs.AsNoTracking().Where(m => teamIds.Contains(m.TeamId) && m.IsValid && m.StartDate <= today &&
            (!m.EndDate.HasValue || m.EndDate >= today) && activeJobs.Any(j => j.IndividualID == m.StaffId))
            .Select(m => new { TargetId=m.TeamId, Person=m.StaffId,
                Name=(m.Staff.Individual.FirstNameEnglish ?? "")+" "+(m.Staff.Individual.MiddleNameEnglish ?? "")+" "+(m.Staff.Individual.LastNameEnglish ?? "") }).ToListAsync();
        static string StaffName(string name) => string.Join(" ",name.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries));
        foreach (var row in departments)
            row.Members = departmentMembers.Where(m => m.TargetId == row.Id).DistinctBy(m => m.Person).Select(m => new OrganisationSettingsOption(m.Person,StaffName(m.Name))).OrderBy(m => m.Name).ToList();
        foreach (var row in teams)
            row.Members = teamMembers.Where(m => m.TargetId == row.Id).DistinctBy(m => m.Person).Select(m => new OrganisationSettingsOption(m.Person,StaffName(m.Name))).OrderBy(m => m.Name).ToList();
        var staff = await db.Staffs.AsNoTracking().Where(x =>
            db.Jobs.Any(j => j.IndividualID == x.IndividualId && j.OrganisationID == organisationId &&
                j.JobStateId == SharedConfig.JobStates.APPROVED && j.TerminatedDate == null && j.JoinedDate <= today) &&
            db.Users.Any(u => u.BusinessEntityID == x.IndividualId && u.IsActive && u.LocalUserStateId == 1 && u.UserOrganisation!.BusinessEntityID == organisationId))
            .OrderBy(x => x.Individual.FirstNameEnglish)
            .Select(x => new OrganisationSettingsOption(x.IndividualId,
                x.Individual.FirstNameEnglish + " " + (x.Individual.MiddleNameEnglish ?? "") + " " + (x.Individual.LastNameEnglish ?? ""))).ToListAsync();
        var assignedPeople = departmentAssignments.Select(x => x.Person).Concat(teamAssignments.Select(x => x.Person)).Distinct().ToList();
        var accountPeople = await db.Users.AsNoTracking().Where(x => x.IsActive && x.LocalUserStateId == 1 && x.UserOrganisation!.BusinessEntityID == organisationId && assignedPeople.Contains(x.BusinessEntityID))
            .Select(x => x.BusinessEntityID).Distinct().ToListAsync();
        var employedPeople = await db.Jobs.AsNoTracking().Where(x => x.OrganisationID == organisationId && assignedPeople.Contains(x.IndividualID) &&
            x.JobStateId == SharedConfig.JobStates.APPROVED && x.TerminatedDate == null && x.JoinedDate <= today)
            .Select(x => x.IndividualID).Distinct().ToListAsync();
        var issues = new Dictionary<int, string>();
        foreach (var person in assignedPeople)
        {
            if (!accountPeople.Contains(person)) issues[person] = "No linked active user account. This supervisor cannot receive approval tasks.";
            else if (!employedPeople.Contains(person)) issues[person] = "No active approved job in this organisation. This supervisor cannot approve handovers.";
        }
        return new()
        {
            Rows = departments.Concat(teams).ToList(), Staff = staff, StructureTypes = structureTypes, SupervisorIssues = issues,
            CanAssignDepartments = await HasTeamPermissionAsync("Organisation.Structure.Supervisor.Add", "Organisation.Edit", "UserOrganisations.Edit"),
            CanRemoveDepartments = await HasTeamPermissionAsync("Organisation.Structure.Supervisor.Edit", "Organisation.Edit", "UserOrganisations.Edit"),
            CanEditTeams = await HasTeamPermissionAsync("Organisation.Teams.Edit", "Organisation.Edit", "UserOrganisations.Edit")
        };
    }

    private static async Task SynchronisePendingHandoversAsync(HrmTeContext db, int organisationId, string kind, int targetId)
    {
        var today = DateTime.UtcNow.AddHours(5).Date;
        var members = db.TeamStaffs.Where(x => x.TeamId == targetId && x.IsValid &&
            x.StartDate <= today && (x.EndDate == null || x.EndDate >= today)).Select(x => x.StaffId);
        var affectedJobs = db.Jobs.Where(x => x.OrganisationID == organisationId &&
            (kind == "Department" ? x.OrganisationStructureId == targetId : members.Contains(x.IndividualID))).Select(x => x.JobId);
        var leaves = await db.Leaves.Where(x => affectedJobs.Contains(x.JobId) &&
            x.LeaveStateId != SharedConfig.LeaveStates.APPROVED && x.LeaveStateId != SharedConfig.LeaveStates.CANCELLED &&
            x.LeaveStateId != SharedConfig.LeaveStates.REJECTED &&
            db.LeaveHandoverReviews.Any(r => r.LeaveId == x.LeaveId && r.SupervisorDecision == "Pending") &&
            db.LeaveRequests.Any(r => r.LeaveId == x.LeaveId && r.Request.RequestStateId != SharedConfig.RequestStates.APPROVED &&
                r.Request.RequestStateId != SharedConfig.RequestStates.CANCELLED && r.Request.RequestStateId != SharedConfig.RequestStates.REJECTED))
            .Select(x => new { x.LeaveId, x.JobId }).ToListAsync();
        foreach (var leave in leaves)
        {
            var eligible = await HRM.Services.Leave.LeaveHandoverService.SupervisorsAsync(db, leave.JobId);
            var current = await db.LeaveHandoverSupervisors.Where(x => x.LeaveId == leave.LeaveId).ToListAsync();
            db.LeaveHandoverSupervisors.RemoveRange(current.Where(x => !eligible.Contains(x.IndividualId)));
            foreach (var person in eligible.Where(x => current.All(s => s.IndividualId != x)))
                db.LeaveHandoverSupervisors.Add(new() { LeaveId = leave.LeaveId, IndividualId = person });
        }
    }

    public async Task<ServiceResult> SetTeamSupervisorAsync(int organisationId, string kind, int teamId, int staffId, bool assign)
    {
        if (kind is not ("Department" or "Team") || organisationId <= 0 || teamId <= 0 || staffId <= 0)
            return ServiceResult.Fail("Select a valid organisation, team and staff member.");
        var permission = kind == "Team" ? "Organisation.Teams.Edit" :
            assign ? "Organisation.Structure.Supervisor.Add" : "Organisation.Structure.Supervisor.Edit";
        await RequireAsync(organisationId, permission, "Organisation.Edit", "UserOrganisations.Edit");
        return ServiceResult.Fail("Submit a supervisor assignment or removal request for approval.");
    }

    internal static async Task<ServiceResult> ValidateSupervisorChangeAsync(HrmTeContext db, IOperationLogService logs, int organisationId, string kind, int teamId, int staffId, bool assign, bool apply)
    {
        var today = DateTime.UtcNow.AddHours(5).Date;
        var targetName = kind == "Department"
            ? await db.OrganisationStructures.Where(x => x.OrganisationStructureId == teamId &&
                x.OrganisationBusinessEntityID == organisationId && x.OrganisationStructureStateId == 3).Select(x => x.Name).SingleOrDefaultAsync()
            : await db.Teams.Where(x => x.TeamId == teamId && x.OrganisationId == organisationId && x.IsValid == true &&
                (x.StartDate == null || x.StartDate <= today) && (x.EndDate == null || x.EndDate >= today)).Select(x => x.Name).SingleOrDefaultAsync();
        if (targetName == null) return ServiceResult.Fail("This team was not found in the selected organisation.");
        if (assign && (!await db.Staffs.AnyAsync(x => x.IndividualId == staffId) ||
            !await db.Jobs.AnyAsync(x => x.IndividualID == staffId && x.OrganisationID == organisationId &&
                x.JobStateId == SharedConfig.JobStates.APPROVED && x.TerminatedDate == null && x.JoinedDate <= today) ||
            !await db.Users.AnyAsync(x => x.BusinessEntityID == staffId && x.IsActive && x.LocalUserStateId == 1 && x.UserOrganisation!.BusinessEntityID == organisationId)))
            return ServiceResult.Fail("A supervisor must have an active approved job and user account in this organisation.");
        if (kind == "Department")
        {
            var current = await db.OrganisationStructureSupervisors.Where(x => x.OrganisationStructureId == teamId &&
                x.SupervisorStaffId == staffId && x.IsActive && x.IsValid).ToListAsync();
            if (assign && current.Count > 0) return ServiceResult.Fail("This staff member is already a supervisor.");
            if (!assign && current.Count == 0) return ServiceResult.Fail("This supervisor assignment is no longer active.");
            if (!apply) return ServiceResult.Ok();
            var log = await logs.CreateAsync(db, assign ? SharedConfig.OperationLogActionTypes.ORGANISATION_STRUCTURE_SUPERVISOR_CREATE :
                SharedConfig.OperationLogActionTypes.ORGANISATION_STRUCTURE_SUPERVISOR_UPDATE,
                $"{(assign ? "Assigned" : "Removed")} staff {staffId} as supervisor of department {teamId} ({targetName}), organisation {organisationId}.");
            if (assign) db.OrganisationStructureSupervisors.Add(new() { OrganisationStructureId = teamId, SupervisorStaffId = staffId, IsActive = true, IsValid = true, OperationLogId = log.OperationLogId });
            else foreach (var row in current) { row.IsActive = false; row.OperationLogId = log.OperationLogId; }
        }
        else
        {
            var memberships = await db.TeamStaffs.Where(x => x.TeamId == teamId && x.StaffId == staffId &&
                x.IsValid && x.StartDate <= today && (x.EndDate == null || x.EndDate >= today)).ToListAsync();
            if (assign && memberships.Any(x => x.IsSuperVisor)) return ServiceResult.Fail("This staff member is already a supervisor.");
            if (!assign && !memberships.Any(x => x.IsSuperVisor)) return ServiceResult.Fail("This supervisor assignment is no longer active.");
            if (!apply) return ServiceResult.Ok();
            var log = await logs.CreateAsync(db, assign ? SharedConfig.OperationLogActionTypes.ORGANISATION_STRUCTURE_SUPERVISOR_CREATE :
                SharedConfig.OperationLogActionTypes.ORGANISATION_STRUCTURE_SUPERVISOR_UPDATE,
                $"{(assign ? "Assigned" : "Removed")} staff {staffId} as supervisor of team {teamId} ({targetName}), organisation {organisationId}.");
            if (assign && memberships.Count == 0)
                db.TeamStaffs.Add(new() { TeamId = teamId, StaffId = staffId, IsSuperVisor = true, IsValid = true, StartDate = today, OperationLogId = log.OperationLogId });
            else if (assign) { memberships[0].IsSuperVisor = true; memberships[0].OperationLogId = log.OperationLogId; }
            else foreach (var row in memberships.Where(x => x.IsSuperVisor)) { row.IsSuperVisor = false; row.OperationLogId = log.OperationLogId; }
        }
        await db.SaveChangesAsync();
        await SynchronisePendingHandoversAsync(db, organisationId, kind, teamId);
        await db.SaveChangesAsync();
        return ServiceResult.Ok(assign ? "Supervisor assigned successfully." : "Supervisor removed successfully.");
    }
}
