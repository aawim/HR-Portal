using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Helpers;

public static class TeamOvertimePolicy
{
    public static async Task<JobTypeOvertimeRule?> ResolveAsync(HrmTeContext db, Job job, DateTime date, CancellationToken ct = default)
    {
        var policy = await db.JobTypeOvertimeRules.AsNoTracking().SingleOrDefaultAsync(p =>
            p.OrganisationId == job.OrganisationID && p.JobTypeId == job.JobTypeId && p.IsAllowed, ct);
        if (policy != null) return policy;
        var team = await db.Teams.AsNoTracking().Where(t => t.ParentTeamId.HasValue && t.OrganisationId == job.OrganisationID &&
                t.IsValid == true && (t.StartDate == null || t.StartDate <= date) && (t.EndDate == null || t.EndDate >= date) &&
                t.OvertimeEnabled && t.MaximumWeeklyOvertimeHours > 0 &&
                t.TeamStaffs.Any(m => m.StaffId == job.IndividualID && m.IsValid && m.StartDate <= date && (!m.EndDate.HasValue || m.EndDate >= date)))
            .OrderBy(t => t.MaximumWeeklyOvertimeHours).ThenBy(t => t.MaximumDailyOvertimeHours).FirstOrDefaultAsync(ct);
        return team == null ? null : new JobTypeOvertimeRule { OrganisationId = job.OrganisationID, JobTypeId = job.JobTypeId,
            IsAllowed = true, MaximumWeeklyHours = team.MaximumWeeklyOvertimeHours!.Value,
            MaximumDailyHours = team.MaximumDailyOvertimeHours, Version = 1 };
    }
}
