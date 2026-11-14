using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public partial class TeamService
{
    private static DateTime TeamToday => DateTime.UtcNow.AddHours(5).Date;

    private async Task<(Team Parent, bool CanManage, int Person, int User)> RequireParentTeamAsync(
        HrmTeContext db, int parentTeamId, CancellationToken ct)
    {
        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        var parent = await db.Teams.SingleOrDefaultAsync(x => x.TeamId == parentTeamId && x.ParentTeamId == null &&
            (actor.IsSuperAdministrator || x.OrganisationId == org), ct) ?? throw new KeyNotFoundException("Parent team not found.");
        var administrator = actor.IsSuperAdministrator || await access.HasPermissionAsync("Admin") || await access.HasPermissionAsync("Organisation.Edit");
        var supervisor = await db.TeamStaffs.AnyAsync(x => x.TeamId == parentTeamId && x.StaffId == actor.IndividualId &&
            x.IsValid && x.IsSuperVisor && x.StartDate <= TeamToday && (!x.EndDate.HasValue || x.EndDate >= TeamToday), ct);
        if (!administrator && !supervisor)
            throw new UnauthorizedAccessException("Only the parent-team supervisor or an authorised administrator can manage sub-teams.");
        return (parent, administrator || supervisor, actor.IndividualId, actor.UserId);
    }

    public async Task<SubTeamWorkspaceDto> LoadSubTeamsAsync(int parentTeamId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var scope = await RequireParentTeamAsync(db, parentTeamId, ct);
        var data = new SubTeamWorkspaceDto { ParentTeamId = parentTeamId, ParentTeamName = scope.Parent.Name, CanManage = scope.CanManage };
        var activeStaffIds = db.Jobs.AsNoTracking()
            .Where(x => x.OrganisationID == scope.Parent.OrganisationId &&
                        x.JobStateId == SharedConfig.JobStates.APPROVED &&
                        x.TerminatedDate == null &&
                        x.JoinedDate <= TeamToday)
            .Select(x => x.IndividualID);
        var activeParentStaffIds = db.TeamStaffs.AsNoTracking()
            .Where(x => x.TeamId == parentTeamId &&
                        x.IsValid &&
                        x.StartDate <= TeamToday &&
                        (!x.EndDate.HasValue || x.EndDate >= TeamToday) &&
                        activeStaffIds.Contains(x.StaffId))
            .Select(x => x.StaffId);
        data.Structures = await db.OrganisationStructures.AsNoTracking()
            .Where(x => x.OrganisationBusinessEntityID == scope.Parent.OrganisationId && x.OrganisationStructureStateId == 3)
            .OrderBy(x => x.Name).Select(x => new SubTeamOptionDto(x.OrganisationStructureId, x.Name)).ToListAsync(ct);

        var assignedSubTeamStaffIds = await db.TeamStaffs.AsNoTracking()
            .Where(ts => db.Teams.Any(t => t.ParentTeamId == parentTeamId && t.IsValid == true && t.TeamId == ts.TeamId) &&
                         ts.IsValid &&
                         ts.StartDate <= TeamToday &&
                         (!ts.EndDate.HasValue || ts.EndDate >= TeamToday) &&
                         activeParentStaffIds.Contains(ts.StaffId))
            .Select(ts => ts.StaffId)
            .Distinct()
            .ToListAsync(ct);

        data.Staff = await db.TeamStaffs.AsNoTracking()
            .Where(x => x.TeamId == parentTeamId && x.IsValid && x.StartDate <= TeamToday &&
                (!x.EndDate.HasValue || x.EndDate >= TeamToday) &&
                activeParentStaffIds.Contains(x.StaffId) &&
                !assignedSubTeamStaffIds.Contains(x.StaffId))
            .OrderBy(x => x.Staff.Individual.FirstNameEnglish)
            .Select(x => new SubTeamOptionDto(x.StaffId,
                (x.Staff.Individual.FirstNameEnglish + " " + (x.Staff.Individual.MiddleNameEnglish ?? "") + " " +
                    (x.Staff.Individual.LastNameEnglish ?? "")).Trim()))
            .Distinct().ToListAsync(ct);
        data.SubTeams = await db.Teams.AsNoTracking().Where(x => x.ParentTeamId == parentTeamId)
            .OrderByDescending(x => x.IsValid).ThenBy(x => x.Name).Select(x => new SubTeamDto
            {
                TeamId = x.TeamId, ParentTeamId = parentTeamId, Name = x.Name, NameDhivehi = x.NameDhivehi ?? "",
                OrganisationId = x.OrganisationId, OrganisationStructureId = x.OrganisationStructureId,
                OrganisationStructureName = db.OrganisationStructures.Where(s => s.OrganisationStructureId == x.OrganisationStructureId).Select(s => s.Name).FirstOrDefault() ?? "",
                OvertimeEnabled = x.OvertimeEnabled, MaximumDailyOvertimeHours = x.MaximumDailyOvertimeHours,
                MaximumWeeklyOvertimeHours = x.MaximumWeeklyOvertimeHours, IsActive = x.IsValid == true,
                Members = x.TeamStaffs.Where(m => m.IsValid && m.StartDate <= TeamToday &&
                    (!m.EndDate.HasValue || m.EndDate >= TeamToday) && activeParentStaffIds.Contains(m.StaffId))
                    .Select(m => new SubTeamMemberDto { StaffId = m.StaffId, Role = m.MemberRole,
                        StartDate = m.StartDate, StaffName = (m.Staff.Individual.FirstNameEnglish + " " + (m.Staff.Individual.LastNameEnglish ?? "")).Trim() }).ToList(),
                Adjustments = db.TeamScheduleAdjustments.Where(a => a.TeamId == x.TeamId).OrderByDescending(a => a.WorkDate).Take(30)
                    .Select(a => new TeamScheduleAdjustmentDto { Id = a.Id, TeamId = a.TeamId, WorkDate = a.WorkDate,
                        RevisedCheckIn = a.RevisedCheckIn, RevisedCheckOut = a.RevisedCheckOut, Reason = a.Reason,
                        IsActive = a.IsActive, Version = a.Version }).ToList()
            }).ToListAsync(ct);
        return data;
    }

    public async Task<int> SaveSubTeamAsync(SaveSubTeamDto model, CancellationToken ct = default)
    {
        model.Name = model.Name.Trim();
        if (model.Name.Length is < 1 or > 200) throw new ArgumentException("Enter a sub-team name of up to 200 characters.");
        if (model.OvertimeEnabled && (model.MaximumWeeklyOvertimeHours is null or <= 0 or > 168 ||
            model.MaximumDailyOvertimeHours is <= 0 or > 24 || model.MaximumDailyOvertimeHours > model.MaximumWeeklyOvertimeHours))
            throw new ArgumentException("Enter valid daily and weekly overtime limits.");
        await using var db = await factory.CreateDbContextAsync(ct);
        var scope = await RequireParentTeamAsync(db, model.ParentTeamId, ct);
        if (model.OrganisationStructureId.HasValue && !await db.OrganisationStructures.AnyAsync(x =>
            x.OrganisationStructureId == model.OrganisationStructureId && x.OrganisationBusinessEntityID == scope.Parent.OrganisationId, ct))
            throw new ArgumentException("Select an organisation structure from the parent team's organisation.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var entity = model.TeamId == 0 ? new Team { ParentTeamId = model.ParentTeamId, OrganisationId = scope.Parent.OrganisationId, StartDate = TeamToday } :
            await db.Teams.SingleOrDefaultAsync(x => x.TeamId == model.TeamId && x.ParentTeamId == model.ParentTeamId, ct)
                ?? throw new KeyNotFoundException("Sub-team not found.");
        if (await db.Teams.AnyAsync(x => x.TeamId != entity.TeamId && x.ParentTeamId == model.ParentTeamId && x.Name == model.Name, ct))
            throw new InvalidOperationException("A sub-team with this name already exists.");
        var audit = await logs.CreateAsync(db, entity.TeamId == 0 ? SharedConfig.OperationLogActionTypes.TEAM_CREATE : SharedConfig.OperationLogActionTypes.TEAM_UPDATE,
            $"Saved sub-team {model.Name} under team {model.ParentTeamId}.");
        if (entity.TeamId == 0) db.Teams.Add(entity);
        var deactivateMembers = !model.IsActive;
        entity.Name = model.Name; entity.NameDhivehi = model.NameDhivehi?.Trim(); entity.OrganisationStructureId = model.OrganisationStructureId;
        entity.OvertimeEnabled = model.IsActive && model.OvertimeEnabled;
        entity.MaximumDailyOvertimeHours = entity.OvertimeEnabled ? model.MaximumDailyOvertimeHours : null;
        entity.MaximumWeeklyOvertimeHours = entity.OvertimeEnabled ? model.MaximumWeeklyOvertimeHours : null;
        entity.IsValid = model.IsActive; entity.EndDate = model.IsActive ? null : TeamToday; entity.OperationLogId = audit.OperationLogId;
        // A deactivated sub-team returns its staff to the parent-team rules.
        if (deactivateMembers)
        {
            var members = await db.TeamStaffs.Where(x => x.TeamId == entity.TeamId && x.IsValid).ToListAsync(ct);
            foreach (var member in members)
            {
                member.IsValid = false;
                member.EndDate = TeamToday;
                member.OperationLogId = audit.OperationLogId;
            }
            var adjustments = await db.TeamScheduleAdjustments.Where(x => x.TeamId == entity.TeamId && x.IsActive).ToListAsync(ct);
            foreach (var adjustment in adjustments)
            {
                adjustment.IsActive = false;
                adjustment.OperationLogId = audit.OperationLogId;
            }
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return entity.TeamId;
    }

    public async Task<bool> DeleteSubTeamAsync(int teamId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var team = await db.Teams.SingleOrDefaultAsync(x => x.TeamId == teamId && x.ParentTeamId.HasValue, ct)
            ?? throw new KeyNotFoundException("Sub-team not found.");
        await RequireParentTeamAsync(db, team.ParentTeamId!.Value, ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.TEAM_UPDATE,
            $"Archived sub-team {team.TeamId}: {team.Name}.");
        team.IsValid = false;
        team.EndDate = TeamToday;
        team.OperationLogId = audit.OperationLogId;
        var members = await db.TeamStaffs.Where(x => x.TeamId == teamId && x.IsValid).ToListAsync(ct);
        foreach (var member in members)
        {
            member.IsValid = false;
            member.EndDate = TeamToday;
            member.OperationLogId = audit.OperationLogId;
        }
        var adjustments = await db.TeamScheduleAdjustments.Where(x => x.TeamId == teamId && x.IsActive).ToListAsync(ct);
        foreach (var adjustment in adjustments)
        {
            adjustment.IsActive = false;
            adjustment.OperationLogId = audit.OperationLogId;
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<int> SaveSubTeamMemberAsync(SaveSubTeamMemberDto model, CancellationToken ct = default)
    {
        var role = model.Role is "Supervisor" or "TeamLead" ? model.Role : "Member";
        await using var db = await factory.CreateDbContextAsync(ct);
        var team = await db.Teams.SingleOrDefaultAsync(x => x.TeamId == model.TeamId && x.ParentTeamId.HasValue, ct)
            ?? throw new KeyNotFoundException("Sub-team not found.");
        if (team.IsValid != true || (team.StartDate.HasValue && team.StartDate > TeamToday) ||
            (team.EndDate.HasValue && team.EndDate < TeamToday))
            throw new InvalidOperationException("Activate the sub-team before assigning staff.");
        var scope = await RequireParentTeamAsync(db, team.ParentTeamId!.Value, ct);
        if (!await db.TeamStaffs.AnyAsync(x => x.TeamId == team.ParentTeamId && x.StaffId == model.StaffId &&
            x.IsValid && x.StartDate <= TeamToday && (!x.EndDate.HasValue || x.EndDate >= TeamToday), ct))
            throw new ArgumentException("Select an active staff member from the parent team.");
        if (!await db.Jobs.AnyAsync(x => x.IndividualID == model.StaffId &&
            x.OrganisationID == scope.Parent.OrganisationId &&
            x.JobStateId == SharedConfig.JobStates.APPROVED &&
            x.TerminatedDate == null && x.JoinedDate <= TeamToday, ct))
            throw new ArgumentException("Select a staff member with an active job in the parent team's organisation.");

        var parentTeamId = team.ParentTeamId.Value;
        var isAlreadyInAnotherSubTeam = await db.TeamStaffs.AnyAsync(ts =>
            ts.StaffId == model.StaffId &&
            ts.TeamId != model.TeamId &&
            ts.IsValid &&
            ts.StartDate <= TeamToday &&
            (!ts.EndDate.HasValue || ts.EndDate >= TeamToday) &&
            db.Teams.Any(t => t.TeamId == ts.TeamId && t.ParentTeamId == parentTeamId && t.IsValid == true),
            ct);

        if (isAlreadyInAnotherSubTeam)
        {
            throw new InvalidOperationException("This staff member already belongs to another sub-team under this team.");
        }

        var row = await db.TeamStaffs.SingleOrDefaultAsync(x => x.TeamId == model.TeamId && x.StaffId == model.StaffId && x.IsValid, ct);
        var audit = await logs.CreateAsync(db, row == null ? SharedConfig.OperationLogActionTypes.TEAM_STAFF_CREATE : SharedConfig.OperationLogActionTypes.TEAM_STAFF_UPDATE,
            $"Assigned staff {model.StaffId} to sub-team {model.TeamId} as {role}.");
        if (row == null) { row = new TeamStaff { TeamId = model.TeamId, StaffId = model.StaffId }; db.TeamStaffs.Add(row); }
        row.StartDate = model.StartDate == default ? TeamToday : model.StartDate.Date; row.EndDate = null; row.IsValid = true;
        row.MemberRole = role; row.IsSuperVisor = role is "Supervisor" or "TeamLead"; row.OperationLogId = audit.OperationLogId;
        await db.SaveChangesAsync(ct); return row.TeamStaffsId;
    }

    public async Task<bool> RemoveSubTeamMemberAsync(int teamId, int staffId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var team = await db.Teams.SingleOrDefaultAsync(x => x.TeamId == teamId && x.ParentTeamId.HasValue, ct)
            ?? throw new KeyNotFoundException("Sub-team not found.");
        await RequireParentTeamAsync(db, team.ParentTeamId!.Value, ct);
        var row = await db.TeamStaffs.SingleOrDefaultAsync(x => x.TeamId == teamId && x.StaffId == staffId && x.IsValid, ct);
        if (row == null) return false;
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.TEAM_STAFF_UPDATE, $"Removed staff {staffId} from sub-team {teamId}.");
        row.IsValid = false; row.EndDate = TeamToday; row.OperationLogId = audit.OperationLogId; await db.SaveChangesAsync(ct); return true;
    }

    public async Task<int> SaveTeamScheduleAdjustmentAsync(TeamScheduleAdjustmentDto model, CancellationToken ct = default)
    {
        if (!model.RevisedCheckIn.HasValue && !model.RevisedCheckOut.HasValue) throw new ArgumentException("Change check-in, check-out, or both.");
        if (string.IsNullOrWhiteSpace(model.Reason) || model.Reason.Length > 1000) throw new ArgumentException("Enter a reason of up to 1,000 characters.");
        await using var db = await factory.CreateDbContextAsync(ct);
        var team = await db.Teams.SingleOrDefaultAsync(x => x.TeamId == model.TeamId && x.ParentTeamId.HasValue, ct)
            ?? throw new KeyNotFoundException("Sub-team not found.");
        var scope = await RequireParentTeamAsync(db, team.ParentTeamId!.Value, ct);
        var entity = model.Id == 0 ? new TeamScheduleAdjustment { TeamId = team.TeamId, CreatedByUserId = scope.User, CreatedAtUtc = DateTime.UtcNow } :
            await db.TeamScheduleAdjustments.SingleOrDefaultAsync(x => x.Id == model.Id && x.TeamId == team.TeamId && x.Version == model.Version, ct)
                ?? throw new InvalidOperationException("This adjustment changed. Refresh and try again.");
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.TEAM_UPDATE,
            $"Adjusted sub-team {team.TeamId} schedule on {model.WorkDate:yyyy-MM-dd}: {model.Reason.Trim()}.");
        if (entity.Id == 0) db.TeamScheduleAdjustments.Add(entity); else entity.Version++;
        entity.WorkDate = model.WorkDate.Date; entity.RevisedCheckIn = model.RevisedCheckIn; entity.RevisedCheckOut = model.RevisedCheckOut;
        entity.Reason = model.Reason.Trim(); entity.IsActive = model.IsActive; entity.OperationLogId = audit.OperationLogId;
        await db.SaveChangesAsync(ct); return entity.Id;
    }
}
