using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public class TeamService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access) : ITeam
{
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
