using HRM.Application.Contracts.Pages;
using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services.Queries;

public sealed class OrganisationDirectoryQueries(IDbContextFactory<HrmTeContext> factory, IUserAccessService access,
    IOrganisationService organisations, ApiReadScope scope) : IOrganisationDirectoryQueries
{
    public async Task<List<OrgNodeDto>> GetApprovedStructureAsync(int? organisationId = null, CancellationToken ct = default)
    {
        var org = await scope.OrganisationAsync(organisationId); ct.ThrowIfCancellationRequested();
        return await organisations.GetOrganisationTreeAsync(org);
    }
    public async Task<List<OrganisationUnitSummary>> GetTeamsAsync(int? organisationId = null, CancellationToken ct = default)
    {
        var org = await scope.OrganisationAsync(organisationId);
        await scope.RequireStaffReadAsync("Employee.View");
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Teams.AsNoTracking().Where(t => t.OrganisationId == org).OrderBy(t => t.Name)
            .Select(t => new OrganisationUnitSummary(t.TeamId, OrganisationUnitKind.Team, org, t.Name, t.NameDhivehi, null,
                t.StartDate, t.EndDate, t.IsValid == true, t.OperationLogId ?? 0)).ToListAsync(ct);
    }
    public async Task<OrganisationUnitDetails?> GetDetailsAsync(OrganisationUnitKey unit, CancellationToken ct = default)
    {
        await scope.RequireStaffReadAsync("Employee.View");
        if (unit.Id <= 0 || !System.Enum.IsDefined(unit.Kind)) throw new ArgumentException("Select a valid organisation unit.");
        await using var db = await factory.CreateDbContextAsync(ct);
        OrganisationUnitSummary? summary = unit.Kind switch
        {
            OrganisationUnitKind.Team => await db.Teams.AsNoTracking().Where(t => t.TeamId == unit.Id)
                .Select(t => new OrganisationUnitSummary(t.TeamId, unit.Kind, t.OrganisationId, t.Name, t.NameDhivehi, null, t.StartDate, t.EndDate, t.IsValid == true, t.OperationLogId ?? 0)).SingleOrDefaultAsync(ct),
            OrganisationUnitKind.Group => await db.Groups.AsNoTracking().Where(g => g.GroupId == unit.Id)
                .Select(g => new OrganisationUnitSummary(g.GroupId, unit.Kind, g.BusinessEntityOrganisationId, g.Name, null, g.GroupTypeId, null, null, true, g.OperationLogId)).SingleOrDefaultAsync(ct),
            _ => await db.OrganisationStructures.AsNoTracking().Where(s => s.OrganisationStructureId == unit.Id && s.OrganisationStructureStateId == 3)
                .Select(s => new OrganisationUnitSummary(s.OrganisationStructureId, unit.Kind, s.OrganisationBusinessEntityID, s.Name, null, s.OrganisationStructureTypeId, null, null, true, s.OperationLogId)).SingleOrDefaultAsync(ct)
        };
        if (summary is null) return null;
        var org = await scope.OrganisationAsync(summary.OrganisationId);
        var today = DateTime.UtcNow.AddHours(5).Date;
        List<OrganisationMember> members;
        if (unit.Kind == OrganisationUnitKind.Team)
            members = await db.TeamStaffs.AsNoTracking().Where(m => m.TeamId == unit.Id && m.IsValid && db.Jobs.Any(j => j.IndividualID == m.StaffId && j.OrganisationID == org))
                .Select(m => new OrganisationMember(m.StaffId, (m.Staff.Individual.FirstNameEnglish + " " + (m.Staff.Individual.LastNameEnglish ?? "")).Trim(),
                    m.Staff.EmployeeNumber, m.StartDate, m.EndDate, m.IsSuperVisor)).ToListAsync(ct);
        else if (unit.Kind == OrganisationUnitKind.Group)
            members = await db.GroupStaffs.AsNoTracking().Where(m => m.GroupId == unit.Id && db.Jobs.Any(j => j.IndividualID == m.StaffIndividualId && j.OrganisationID == org))
                .Select(m => new OrganisationMember(m.StaffIndividualId, (m.StaffIndividual.Individual.FirstNameEnglish + " " + (m.StaffIndividual.Individual.LastNameEnglish ?? "")).Trim(),
                    m.StaffIndividual.EmployeeNumber, m.EffectiveStartDate, m.EffectiveEndDate, false)).ToListAsync(ct);
        else
            members = await db.Jobs.AsNoTracking().Where(j => j.OrganisationID == org && j.OrganisationStructureId == unit.Id && j.JobStateId == 4)
                .Select(j => new OrganisationMember(j.IndividualID, (j.Individual.FirstNameEnglish + " " + (j.Individual.LastNameEnglish ?? "")).Trim(), j.Staff.EmployeeNumber,
                    j.JoinedDate, j.TerminatedDate, db.OrganisationStructureSupervisors.Any(s => s.OrganisationStructureId == unit.Id && s.SupervisorStaffId == j.IndividualID && s.IsActive && s.IsValid))).ToListAsync(ct);
        var current = members.Where(m => m.StartsOn <= today && (!m.EndsOn.HasValue || m.EndsOn > today)).OrderBy(m => m.Name).ToList();
        var previous = members.Where(m => m.EndsOn <= today).OrderByDescending(m => m.EndsOn).ToList();
        var history = new List<OrganisationMembershipPeriod>();
        if (members.Count > 0)
        {
            var first = members.Min(m => m.StartsOn);
            for (var month = new DateTime(first.Year, first.Month, 1); month <= today; month = month.AddMonths(1))
            {
                var next = month.AddMonths(1);
                history.Add(new(DateOnly.FromDateTime(month), members.Count(m => m.StartsOn >= month && m.StartsOn < next),
                    members.Count(m => m.EndsOn >= month && m.EndsOn < next), members.Count(m => m.StartsOn < next && (!m.EndsOn.HasValue || m.EndsOn >= next))));
            }
        }
        var assignments = await db.ScopeScheduleAssignments.AsNoTracking().Where(a => a.OrganisationId == org &&
            (a.GroupId == null && a.OrganisationStructureId == null || unit.Kind == OrganisationUnitKind.Group && a.GroupId == unit.Id ||
             unit.Kind == OrganisationUnitKind.Structure && a.OrganisationStructureId == unit.Id))
            .Select(a => new { a.Id, a.WorkTemplateId, a.WorkTemplate.Name, a.SchedulePurpose, a.EffectiveFrom, a.EffectiveTo, a.IsActive, a.Version }).ToListAsync(ct);
        var schedules = assignments.Select(a => new OrganisationUnitSchedule(a.Id, a.WorkTemplateId, a.Name, a.SchedulePurpose,
            DateOnly.FromDateTime(a.EffectiveFrom), a.EffectiveTo.HasValue ? DateOnly.FromDateTime(a.EffectiveTo.Value) : null, a.IsActive, a.Version)).ToList();
        var leaveSets = unit.Kind != OrganisationUnitKind.Group ? [] : await db.GroupLeaveSets.AsNoTracking().Where(l => l.GroupId == unit.Id)
            .Select(l => new OrganisationUnitLeaveSet(l.GroupLeaveSetId, l.LeaveSetId, l.LeaveSet.Name, l.LeaveSet.IsPublic, l.LeaveSet.IsGlobal, l.IsValid,
                l.LeaveSet.LeaveSetLeaveTypes.Where(t => t.IsValid).Select(t => t.LeaveType.Name).ToList())).ToListAsync(ct);
        var available = await db.LeaveSets.AsNoTracking().Where(l => l.OrganisationId == org || l.IsPublic || l.IsGlobal)
            .OrderBy(l => l.Name).Select(l => new DirectoryOption(l.LeaveSetId, l.Name)).ToListAsync(ct);
        var canManage = (await access.RequireContextAsync()).IsSuperAdministrator || await access.HasPermissionAsync("Employee.Edit") || await access.HasPermissionAsync("Admin");
        return new(summary, current, previous, history, schedules, leaveSets, available, canManage);
    }
}
