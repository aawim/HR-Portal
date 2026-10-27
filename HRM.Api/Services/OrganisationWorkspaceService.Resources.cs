using HRM.DTOs;
using HRM.Enum;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public partial class OrganisationWorkspaceService
{
    public async Task<List<OrganisationWorkspaceResource>> ResourcesAsync(int organisationId, WorkspaceTab tab)
    {
        await RequireAsync(organisationId);
        var actor = await access.RequireContextAsync();
        if (tab is WorkspaceTab.AssignedRoles or WorkspaceTab.Permissions)
        {
            var allowed = actor.IsSuperAdministrator || await access.HasPermissionAsync("Admin") ||
                await access.HasPermissionAsync("Users.Roles.View") || await access.HasPermissionAsync("Users.Roles.Edit") ||
                await access.HasPermissionAsync("UserOrganisations.Admin.View") || await access.HasPermissionAsync("UserOrganisation.Groups.View");
            if (!allowed) throw new UnauthorizedAccessException("You do not have permission to view organisation roles and permissions.");
        }
        await using var db = await factory.CreateDbContextAsync();
        var today = Today;
        if (tab == WorkspaceTab.AssignedRoles)
        {
            var groups = await db.UserGroups.AsNoTracking().Where(x => x.UserOrganisation.BusinessEntityID == organisationId ||
                x.UserOrganisationUserGroups.Any(a => a.IsActive && a.UserOrganisation.BusinessEntityID == organisationId))
                .OrderBy(x => x.GroupName).Select(x => new
                {
                    x.UserGroupId, x.GroupName,
                    Users = x.UserAssignedUserGroups.Where(a => a.User.IsActive && a.User.UserOrganisation != null && a.User.UserOrganisation.BusinessEntityID == organisationId).Select(a => a.User.FullName ?? a.User.Username).ToList(),
                    Permissions = x.UserGroupRoles.Where(r => r.IsActive).Select(r => r.Role.RoleKey).ToList()
                }).ToListAsync();
            return groups.Select(x => new OrganisationWorkspaceResource { Id = x.UserGroupId, Name = x.GroupName,
                Summary = $"{x.Users.Count} assigned active accounts · {x.Permissions.Count} permissions",
                Details = x.Users.Order().Select(n => "Account: " + n).Concat(x.Permissions.Order().Select(p => "Permission: " + p)).ToList() }).ToList();
        }
        if (tab == WorkspaceTab.Permissions)
        {
            return await db.UserOrganisationRoles.AsNoTracking().Where(x => x.IsActive && x.UserOrganisation.BusinessEntityID == organisationId)
                .OrderBy(x => x.Role.RoleKey).Select(x => new OrganisationWorkspaceResource
                { Id = x.RoleID, Name = x.Role.Name, Summary = x.Role.RoleKey, Details = new List<string> { x.Role.Description ?? "" } }).ToListAsync();
        }
        if (tab == WorkspaceTab.Teams)
        {
            var teams = await db.Teams.AsNoTracking().Where(x => x.OrganisationId == organisationId && x.IsValid == true &&
                (x.StartDate == null || x.StartDate <= today) && (x.EndDate == null || x.EndDate >= today))
                .OrderBy(x => x.Name).Select(x => new
                {
                    x.TeamId, x.Name,
                    Members = x.TeamStaffs.Where(m => m.IsValid && m.StartDate <= today && (m.EndDate == null || m.EndDate >= today) &&
                        db.Jobs.Any(j => j.IndividualID == m.StaffId && j.OrganisationID == organisationId && j.JobStateId == HRM.Components.Shared.SharedConfig.JobStates.APPROVED && j.TerminatedDate == null && j.JoinedDate <= today))
                        .Select(m => new { m.StaffId, m.IsSuperVisor, Name = (m.Staff.Individual.FirstNameEnglish + " " + (m.Staff.Individual.MiddleNameEnglish ?? "") + " " + (m.Staff.Individual.LastNameEnglish ?? "")).Trim() }).ToList()
                }).ToListAsync();
            return teams.Select(x => new OrganisationWorkspaceResource { Id = x.TeamId, Name = x.Name,
                Summary = $"{x.Members.Select(m => m.StaffId).Distinct().Count()} active members",
                Details = x.Members.GroupBy(m => m.StaffId).Select(g => g.OrderByDescending(m => m.IsSuperVisor).First())
                    .OrderByDescending(m => m.IsSuperVisor).ThenBy(m => m.Name).Select(m => m.Name + (m.IsSuperVisor ? " · Supervisor" : " · Team member")).ToList() }).ToList();
        }
        if (tab == WorkspaceTab.LeaveType)
        {
            var types = await db.LeaveTypes.AsNoTracking().Where(x => x.OrganisationId == organisationId ||
                db.LeaveSetLeaveTypes.Any(a => a.LeaveTypeId == x.LeaveTypeId && a.IsValid && a.LeaveSet.OrganisationId == organisationId) ||
                db.JobLeaveTypes.Any(a => a.LeaveTypeId == x.LeaveTypeId && a.IsValid && a.Job.OrganisationID == organisationId))
                .OrderBy(x => x.Name).Select(x => new
                {
                    x.LeaveTypeId, x.Name, x.Duration, x.IncludeHolidays, x.IncludePay,
                    Assigned = db.JobLeaveTypes.Where(a => a.LeaveTypeId == x.LeaveTypeId && a.IsValid &&
                        (a.EffectiveFromDate == null || a.EffectiveFromDate <= today) && (a.EffectiveToDate == null || a.EffectiveToDate >= today) &&
                        a.Job.OrganisationID == organisationId && a.Job.JobStateId == HRM.Components.Shared.SharedConfig.JobStates.APPROVED && a.Job.TerminatedDate == null && a.Job.JoinedDate <= today)
                        .Select(a => a.Job.IndividualID).Distinct().Count()
                }).ToListAsync();
            return types.Select(x => new OrganisationWorkspaceResource { Id = x.LeaveTypeId, Name = x.Name,
                Summary = $"{x.Assigned} assigned staff · {(x.Duration.HasValue ? x.Duration + " days" : "Policy-defined duration")}",
                Details = [x.IncludePay ? "Paid leave" : "Unpaid leave", x.IncludeHolidays ? "Holidays included" : "Holidays excluded"] }).ToList();
        }
        return [];
    }
}
