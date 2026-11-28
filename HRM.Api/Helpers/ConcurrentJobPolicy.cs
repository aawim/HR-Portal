using HRM.Models;
using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;

namespace HRM.Helpers;

/// <summary>
/// Enforces concurrent employment rules: a staff member may hold concurrent jobs only
/// when they have different Job Types (e.g., 1 Permanent + 1 Contract) and their
/// effective working hours do not overlap.
/// </summary>
public static class ConcurrentJobPolicy
{
    public static async Task<string?> ValidateWorkingHoursOverlapAsync(
        HrmTeContext db,
        int individualId,
        int targetJobId,
        DateTime from,
        DateTime? to,
        WorkTemplate? proposedTemplate = null,
        int? proposedWorkingDaysMask = null,
        CancellationToken ct = default)
    {
        var windowFrom = from.Date;
        var windowTo = to?.Date ?? windowFrom.AddYears(1);

        var otherJobs = await db.Jobs
            .AsNoTracking()
            .Include(j => j.JobType)
            .Where(j => j.IndividualID == individualId
                && j.JobId != targetJobId
                && j.JobStateId == 4
                && (!j.TerminatedDate.HasValue || j.TerminatedDate.Value > windowFrom)
                && (!to.HasValue || j.JoinedDate < to.Value))
            .ToListAsync(ct);

        if (otherJobs.Count == 0)
            return null;

        var targetWindows = new List<(TimeOnly Start, TimeOnly End, bool EndsNextDay, int DaysMask, string Label)>();
        if (proposedTemplate is { DefaultStartTime: { } ps, DefaultEndTime: { } pe })
        {
            targetWindows.Add((ps, pe, proposedTemplate.EndsNextDay, proposedWorkingDaysMask ?? 31, proposedTemplate.Name));
        }
        else
        {
            var targetJob = await db.Jobs.AsNoTracking().SingleOrDefaultAsync(j => j.JobId == targetJobId, ct);
            if (targetJob == null) return null;
            var targetSchedules = await EffectiveStaffSchedules.LoadAsync(db, targetJob, windowFrom, windowTo, ct);
            foreach (var c in targetSchedules.Where(x => x.Assignment.SchedulePurpose == "Official"))
            {
                if (c.Assignment.WorkTemplate is { DefaultStartTime: { } s, DefaultEndTime: { } e })
                {
                    var mask = c.Assignment.WorkingDaysMask is >= 1 and <= 127 ? c.Assignment.WorkingDaysMask : 31;
                    targetWindows.Add((s, e, c.Assignment.WorkTemplate.EndsNextDay, mask, c.Assignment.WorkTemplate.Name));
                }
            }
        }

        if (targetWindows.Count == 0)
            return null;

        foreach (var other in otherJobs)
        {
            var otherSchedules = await EffectiveStaffSchedules.LoadAsync(db, other, windowFrom, windowTo, ct);
            foreach (var oc in otherSchedules.Where(x => x.Assignment.SchedulePurpose == "Official"))
            {
                if (oc.Assignment.WorkTemplate is not { DefaultStartTime: { } os, DefaultEndTime: { } oe })
                    continue;
                var otherMask = oc.Assignment.WorkingDaysMask is >= 1 and <= 127 ? oc.Assignment.WorkingDaysMask : 31;

                foreach (var tw in targetWindows)
                {
                    if ((tw.DaysMask & otherMask) == 0)
                        continue;

                    if (TimesOverlap(tw.Start, tw.End, tw.EndsNextDay, os, oe, oc.Assignment.WorkTemplate.EndsNextDay))
                    {
                        var otherType = other.JobType?.TypeName ?? "active";
                        return $"Working hours overlap with active {otherType} job (#{other.JobId}): {tw.Start:HH:mm}–{tw.End:HH:mm} conflicts with {os:HH:mm}–{oe:HH:mm}. Concurrent jobs cannot have overlapping working hours.";
                    }
                }
            }
        }

        return null;
    }

    private static bool TimesOverlap(
        TimeOnly startA, TimeOnly endA, bool nextDayA,
        TimeOnly startB, TimeOnly endB, bool nextDayB)
    {
        var aStart = startA.ToTimeSpan();
        var aEnd = endA.ToTimeSpan();
        if (nextDayA || aEnd <= aStart) aEnd += TimeSpan.FromHours(24);

        var bStart = startB.ToTimeSpan();
        var bEnd = endB.ToTimeSpan();
        if (nextDayB || bEnd <= bStart) bEnd += TimeSpan.FromHours(24);

        return aStart < bEnd && bStart < aEnd;
    }
}
