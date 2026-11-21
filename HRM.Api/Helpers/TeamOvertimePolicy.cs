using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Helpers;

public static class TeamOvertimePolicy
{
    public static async Task<JobTypeOvertimeRule?> ResolveAsync(HrmTeContext db, Job job, DateTime date, CancellationToken ct = default)
    {
        var day = date.Date;

        // 1. Independent Staff Group override (highest priority while active and within effective dates)
        if (db.Model.FindEntityType(typeof(StaffGroupMember)) != null)
        {
            var sg = await db.StaffGroupMembers.AsNoTracking()
                .Where(m => m.OrganisationId == job.OrganisationID && m.IndividualId == job.IndividualID && m.IsActive &&
                            m.StaffGroup.IsActive && !m.StaffGroup.IsDissolved && m.StaffGroup.OvertimeEnabled.HasValue &&
                            m.StaffGroup.EffectiveFrom <= day && (!m.StaffGroup.EffectiveTo.HasValue || m.StaffGroup.EffectiveTo >= day))
                .OrderByDescending(m => m.StaffGroup.EffectiveFrom)
                .ThenByDescending(m => m.StaffGroupId)
                .Select(m => new { m.StaffGroup.OvertimeEnabled, m.StaffGroup.MaximumDailyOvertimeHours, m.StaffGroup.MaximumWeeklyOvertimeHours })
                .FirstOrDefaultAsync(ct);
            if (sg != null)
            {
                if (sg.OvertimeEnabled == false) return null;
                return new JobTypeOvertimeRule
                {
                    OrganisationId = job.OrganisationID,
                    JobTypeId = job.JobTypeId,
                    IsAllowed = true,
                    MaximumWeeklyHours = sg.MaximumWeeklyOvertimeHours ?? 40m,
                    MaximumDailyHours = sg.MaximumDailyOvertimeHours,
                    Version = 1
                };
            }
        }

        // 2. Sub-Group / Sub-Team override (overrides Job Type OT while active and within sub-team & membership dates)
        var team = await db.Teams.AsNoTracking().Where(t => t.ParentTeamId.HasValue && t.OrganisationId == job.OrganisationID &&
                t.IsValid == true && (t.StartDate == null || t.StartDate <= day) && (t.EndDate == null || t.EndDate >= day) &&
                t.OvertimeEnabled && t.MaximumWeeklyOvertimeHours > 0 &&
                t.TeamStaffs.Any(m => m.StaffId == job.IndividualID && m.IsValid && m.StartDate <= day && (!m.EndDate.HasValue || m.EndDate >= day)))
            .OrderBy(t => t.MaximumWeeklyOvertimeHours).ThenBy(t => t.MaximumDailyOvertimeHours).FirstOrDefaultAsync(ct);
        if (team != null)
        {
            return new JobTypeOvertimeRule
            {
                OrganisationId = job.OrganisationID,
                JobTypeId = job.JobTypeId,
                IsAllowed = true,
                MaximumWeeklyHours = team.MaximumWeeklyOvertimeHours!.Value,
                MaximumDailyHours = team.MaximumDailyOvertimeHours,
                Version = 1
            };
        }

        // 3. Original Job Type OT rule (automatic fallback once any group/sub-group expires or member leaves)
        return await db.JobTypeOvertimeRules.AsNoTracking().SingleOrDefaultAsync(p =>
            p.OrganisationId == job.OrganisationID && p.JobTypeId == job.JobTypeId && p.IsAllowed, ct);
    }
}
