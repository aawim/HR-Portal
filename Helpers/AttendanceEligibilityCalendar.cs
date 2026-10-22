using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Helpers;

public sealed record AttendanceEligibilityDay(bool HasApprovedPosition, bool HasActiveSchedule, bool IsScheduledDay, bool RequiresAttendance)
{
    public const string NoScheduleMessage = "No active schedule found";
    public bool CanBeAbsent => HasApprovedPosition && HasActiveSchedule && IsScheduledDay && RequiresAttendance;
    public string? Message => !HasActiveSchedule ? NoScheduleMessage : !HasApprovedPosition ? "Attendance starts from job position approval" : null;
}

/// <summary>Shared eligibility rule for calendars, daily evaluations and organisation reports.</summary>
public static class AttendanceEligibilityCalendar
{
    public static async Task<Dictionary<DateTime, AttendanceEligibilityDay>> LoadAsync(HrmTeContext db, int individualId,
        DateTime from, DateTime to, int? organisationId = null, int? jobId = null, CancellationToken ct = default)
    {
        from = from.Date; to = to.Date;
        var jobs = await db.Jobs.AsNoTracking().Where(j => j.IndividualID == individualId && j.JobStateId == 4 &&
            (!organisationId.HasValue || j.OrganisationID == organisationId) && (!jobId.HasValue || j.JobId == jobId) &&
            j.JoinedDate <= to && (!j.TerminatedDate.HasValue || j.TerminatedDate >= from))
            .Select(j => new { j.JobId, j.JoinedDate, j.TerminatedDate }).ToListAsync(ct);
        var jobIds = jobs.Select(j => j.JobId).ToList();
        var positions = await db.JobPositions.AsNoTracking().Where(p => jobIds.Contains(p.JobId) && p.JobPositionStateId == 4 &&
            p.FromDate <= to && (!p.ToDate.HasValue || p.ToDate >= from))
            .Select(p => new { p.JobPositionId, p.JobId, p.FromDate, p.ToDate }).ToListAsync(ct);
        var positionIds = positions.Select(p => p.JobPositionId).ToList();
        var approvalDates = await (from link in db.JobPositionsRequests.AsNoTracking()
                                   join request in db.Requests.AsNoTracking() on link.RequestId equals request.RequestId
                                   where positionIds.Contains(link.JobPositionId) && request.RequestStateId == 4 && request.RequestTypeId == 32
                                   select new { link.JobPositionId, request.LastStateChangedDate }).ToListAsync(ct);

        var assignments = await db.JobWorkTemplateAssignments.AsNoTracking().Include(a => a.WorkTemplate).ThenInclude(t => t.WorkTemplateSegments)
            .Where(a => jobIds.Contains(a.JobID) && a.IsActive && a.EffectiveFrom <= to && (!a.EffectiveTo.HasValue || a.EffectiveTo >= from))
            .ToListAsync(ct);
        var legacy = await db.JobWorkTemplates.AsNoTracking().Include(a => a.WorkTemplate).ThenInclude(t => t.WorkTemplateSegments)
            .Where(a => jobIds.Contains(a.JobId) && a.IsActive && a.EffectiveFrom <= to && (!a.EffectiveTo.HasValue || a.EffectiveTo >= from))
            .ToListAsync(ct);
        var plans = await db.WorkPlans.AsNoTracking().Where(p => p.IndividualId == individualId && jobIds.Contains(p.JobId) && p.IsValid &&
            p.WorkDate >= from && p.WorkDate <= to)
            .Select(p => new { p.JobId, p.WorkDate,
                HasSchedule = p.WorkAssignments.Any(a => a.IsValid) || p.WorkPlanSegments.Any(s => s.IsValid),
                RequiresAttendance = p.WorkAssignments.Any(a => a.IsValid && a.RequiresAttendance) || p.WorkPlanSegments.Any(s => s.IsValid && s.RequiresAttendance) })
            .ToListAsync(ct);

        bool PositionApproved(int id, DateTime day)
        {
            var job = jobs.First(j => j.JobId == id);
            if (day < job.JoinedDate.Date || job.TerminatedDate.HasValue && day >= job.TerminatedDate.Value.Date) return false;
            return positions.Where(p => p.JobId == id).Any(p =>
            {
                // Legacy approved records without linked approval history use their
                // recorded effective date. Known approval dates never backdate absence.
                var approved = approvalDates.Where(a => a.JobPositionId == p.JobPositionId && a.LastStateChangedDate.Year >= 1900)
                    .Select(a => a.LastStateChangedDate.AddHours(5).Date).DefaultIfEmpty(p.FromDate.Date).Max();
                return day >= p.FromDate.Date && day >= approved && (!p.ToDate.HasValue || day <= p.ToDate.Value.Date);
            });
        }
        static bool TemplateValid(HRM.Models.WorkPlanning.WorkTemplate? t, DateTime day) => t?.IsActive == true &&
            (!t.EffectiveFrom.HasValue || t.EffectiveFrom.Value.Date <= day) && (!t.EffectiveTo.HasValue || t.EffectiveTo.Value.Date >= day);
        static bool TemplateRequires(HRM.Models.WorkPlanning.WorkTemplate t) => t.RequiresAttendance || t.WorkTemplateSegments.Any(s => s.IsActive && s.RequiresAttendance);

        var result = new Dictionary<DateTime, AttendanceEligibilityDay>();
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var eligibleJobs = jobs.Where(j => PositionApproved(j.JobId, day)).Select(j => j.JobId).ToHashSet();
            var active = assignments.Where(a => a.EffectiveFrom.Date <= day && (!a.EffectiveTo.HasValue || a.EffectiveTo.Value.Date >= day))
                .GroupBy(a => new { a.JobID, a.SchedulePurpose }).Select(g => g.OrderByDescending(a => a.EffectiveFrom).ThenByDescending(a => a.JobWorkTemplateAssignmentID).First())
                .Where(a => TemplateValid(a.WorkTemplate, day)).ToList();
            var old = legacy.Where(a => a.EffectiveFrom.Date <= day && (!a.EffectiveTo.HasValue || a.EffectiveTo.Value.Date >= day) &&
                    !active.Any(n => n.JobID == a.JobId && n.SchedulePurpose == "Official") && TemplateValid(a.WorkTemplate, day))
                .GroupBy(a => a.JobId).Select(g => g.OrderByDescending(a => a.Priority).ThenByDescending(a => a.EffectiveFrom).ThenByDescending(a => a.JobWorkTemplateId).First()).ToList();
            var dated = plans.Where(p => p.WorkDate.Date == day && p.HasSchedule).ToList();
            var hasSchedule = active.Count > 0 || old.Count > 0 || dated.Count > 0;
            var required = active.Any(a => eligibleJobs.Contains(a.JobID) && StaffScheduleCalendar.TemplateOn(a, day) > 0 && TemplateRequires(a.WorkTemplate)) ||
                old.Any(a => eligibleJobs.Contains(a.JobId) && LegacyWorks(a, day) && TemplateRequires(a.WorkTemplate)) ||
                dated.Any(p => eligibleJobs.Contains(p.JobId) && p.RequiresAttendance);
            var working = active.Any(a => StaffScheduleCalendar.TemplateOn(a, day) > 0) || old.Any(a => LegacyWorks(a, day)) || dated.Any(p => p.RequiresAttendance);
            result[day] = new(eligibleJobs.Count > 0, hasSchedule, working, required);
        }
        return result;
    }

    private static bool LegacyWorks(JobWorkTemplate a, DateTime day) => day.DayOfWeek switch
    {
        DayOfWeek.Sunday => a.Sunday, DayOfWeek.Monday => a.Monday, DayOfWeek.Tuesday => a.Tuesday,
        DayOfWeek.Wednesday => a.Wednesday, DayOfWeek.Thursday => a.Thursday, DayOfWeek.Friday => a.Friday, _ => a.Saturday
    };
}
