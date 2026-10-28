using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public class TeamService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access, IOperationLogService logs) : ITeam
{
    public async Task<int> CreateAsync(int organisationId, string name, string? nameDhivehi, DateTime startDate, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        if (!actor.IsSuperAdministrator && (organisationId != await access.GetCurrentOrganisationIdAsync() ||
            !await access.HasOrganisationAccessAsync(organisationId))) throw new UnauthorizedAccessException();
        if (!actor.IsSuperAdministrator && !await access.HasPermissionAsync("Admin") && !await access.HasPermissionAsync("Organisation.Edit"))
            throw new UnauthorizedAccessException();
        name = name.Trim(); nameDhivehi = nameDhivehi?.Trim();
        if (organisationId <= 0 || name.Length is < 1 or > 200 || nameDhivehi?.Length > 200 || startDate == default)
            throw new ArgumentException("Enter a team name and start date.");
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        if (!await db.Organisations.AnyAsync(o => o.BusinessEntityID == organisationId, ct)) throw new ArgumentException("Organisation unavailable.");
        if (await db.Teams.AnyAsync(t => t.OrganisationId == organisationId && t.Name == name, ct)) throw new InvalidOperationException("This team already exists.");
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.TEAM_CREATE, $"Create team {name} in organisation {organisationId}.");
        var entity = new Team { OrganisationId = organisationId, Name = name, NameDhivehi = nameDhivehi,
            StartDate = startDate.Date, IsValid = true, OperationLogId = audit.OperationLogId };
        db.Teams.Add(entity); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return entity.TeamId;
    }
    public async Task<List<TeamDto>> GetMyTeamMembersAsync(int teamId)
        => (await GetMyTeamsAsync()).Where(x => x.TeamId == teamId).ToList();

    public async Task<List<TeamDto>> GetMyTeamsAsync()
    {
        var actor = await access.RequireContextAsync();
        if (actor.ActiveJob == null) return [];
        await using var db = await factory.CreateDbContextAsync();
        var today = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Indian/Maldives").Date;
        var organisationId = await db.Jobs.Where(x => x.JobId == actor.ActiveJob.JobId && x.IndividualID == actor.IndividualId &&
            x.JobStateId == SharedConfig.JobStates.APPROVED && x.TerminatedDate == null && x.JoinedDate <= today)
            .Select(x => (int?)x.OrganisationID).SingleOrDefaultAsync();
        if (organisationId == null) return [];
        var teamIds = db.TeamStaffs.Where(x => x.StaffId == actor.IndividualId && x.IsSuperVisor && x.IsValid &&
            x.StartDate <= today && (x.EndDate == null || x.EndDate >= today)).Select(x => x.TeamId);
        var teams = await db.Teams.AsNoTracking().Where(x => teamIds.Contains(x.TeamId) && x.OrganisationId == organisationId &&
            x.IsValid == true && (x.StartDate == null || x.StartDate <= today) && (x.EndDate == null || x.EndDate >= today))
            .OrderBy(x => x.Name).Select(x => new TeamDto
            {
                TeamId = x.TeamId, Name = x.Name, NameDhivehi = x.NameDhivehi ?? "",
                Staffs = x.TeamStaffs.Where(m => m.IsValid && m.StartDate <= today && (m.EndDate == null || m.EndDate >= today) &&
                    db.Jobs.Any(j => j.IndividualID == m.StaffId && j.OrganisationID == organisationId &&
                        j.JobStateId == SharedConfig.JobStates.APPROVED && j.TerminatedDate == null && j.JoinedDate <= today))
                    .Select(m => new TeamStaffDto
                    {
                        StaffId = m.StaffId, IsSuperVisor = m.IsSuperVisor,
                        FullName = (m.Staff.Individual.FirstNameEnglish + " " + (m.Staff.Individual.MiddleNameEnglish ?? "") + " " + (m.Staff.Individual.LastNameEnglish ?? "")).Trim()
                    }).ToList()
            }).ToListAsync();
        foreach (var team in teams)
            team.Staffs = team.Staffs.GroupBy(x => x.StaffId).Select(x => x.OrderByDescending(m => m.IsSuperVisor).First())
                .OrderByDescending(x => x.IsSuperVisor).ThenBy(x => x.FullName).ToList();
        return teams;
    }
}
