using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Helpers;

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
            .ToListAsync(ct);
        var jobIds = jobs.Select(j => j.JobId).ToList();
        var positions = await db.JobPositions.AsNoTracking().Where(p => jobIds.Contains(p.JobId) && p.JobPositionStateId == 4 &&
            p.FromDate <= to && (!p.ToDate.HasValue || p.ToDate >= from))
            .Select(p => new { p.JobPositionId, p.JobId, p.FromDate, p.ToDate }).ToListAsync(ct);
        var positionIds = positions.Select(p => p.JobPositionId).ToList();
        var approvalDates = await (from link in db.JobPositionsRequests.AsNoTracking()
                                   join request in db.Requests.AsNoTracking() on link.RequestId equals request.RequestId
                                   where positionIds.Contains(link.JobPositionId) && request.RequestStateId == 4
                                   select new { link.JobPositionId, request.LastStateChangedDate }).ToListAsync(ct);

        var effective = new Dictionary<int,List<StaffScheduleCandidate>>();
        foreach(var job in jobs) effective[job.JobId]=await EffectiveStaffSchedules.LoadAsync(db,job,from,to,ct);
        var plans = await db.WorkPlans.AsNoTracking().Where(p => p.IndividualId == individualId && jobIds.Contains(p.JobId) && p.IsValid &&
            p.WorkDate >= from && p.WorkDate <= to)
            .Select(p => new { p.JobId, p.WorkDate,
                HasSchedule = p.WorkAssignments.Any(a => a.IsValid && a.SchedulePurpose == "Official") || p.WorkPlanSegments.Any(s => s.IsValid && s.SchedulePurpose == "Official"),
                RequiresAttendance = p.WorkAssignments.Any(a => a.IsValid && a.SchedulePurpose == "Official" && a.RequiresAttendance) || p.WorkPlanSegments.Any(s => s.IsValid && s.SchedulePurpose == "Official" && s.RequiresAttendance) })
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
                return day >= approved && PositionLifecycle.Contains(p.FromDate,p.ToDate,day);
            });
        }
        static bool TemplateValid(HRM.Models.WorkPlanning.WorkTemplate? t, DateTime day) => t?.IsActive == true &&
            (!t.EffectiveFrom.HasValue || t.EffectiveFrom.Value.Date <= day) && (!t.EffectiveTo.HasValue || t.EffectiveTo.Value.Date >= day);
        static bool TemplateRequires(HRM.Models.WorkPlanning.WorkTemplate t) => t.RequiresAttendance || t.WorkTemplateSegments.Any(s => s.IsActive && s.RequiresAttendance);

        var result = new Dictionary<DateTime, AttendanceEligibilityDay>();
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var eligibleJobs = jobs.Where(j => PositionApproved(j.JobId, day)).Select(j => j.JobId).ToHashSet();
            var active = effective.Values.SelectMany(list=>EffectiveStaffSchedules.On(list,day)).Select(c=>c.Assignment)
                .Where(a=>a.SchedulePurpose=="Official" && TemplateValid(a.WorkTemplate,day)).ToList();
            var dated = plans.Where(p => p.WorkDate.Date == day && p.HasSchedule).ToList();
            var hasSchedule = active.Count > 0 || dated.Count > 0;
            var required = active.Any(a => eligibleJobs.Contains(a.JobID) && StaffScheduleCalendar.TemplateOn(a, day) > 0 && TemplateRequires(a.WorkTemplate)) ||
                dated.Any(p => eligibleJobs.Contains(p.JobId) && p.RequiresAttendance);
            var working = active.Any(a => StaffScheduleCalendar.TemplateOn(a, day) > 0) || dated.Any(p => p.RequiresAttendance);
            result[day] = new(eligibleJobs.Count > 0, hasSchedule, working, required);
        }
        return result;
    }

}
