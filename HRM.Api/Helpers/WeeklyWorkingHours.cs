using HRM.DTOs.Attendance;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Helpers;

public static class WeeklyWorkingHours
{
    public static async Task<WeeklyWorkedHoursDto> LoadAsync(HrmTeContext db, int person, int org, DateTime date, CancellationToken ct = default)
    {
        var jobs = await db.Jobs.AsNoTracking().Where(j => j.IndividualID == person && j.OrganisationID == org && j.JobStateId == 4 &&
            j.JoinedDate <= date.Date.AddDays(6) && (!j.TerminatedDate.HasValue || j.TerminatedDate >= date.Date.AddDays(-6)))
            .OrderByDescending(j => j.JoinedDate <= date.Date && (!j.TerminatedDate.HasValue || j.TerminatedDate >= date.Date))
            .ThenByDescending(j => j.JoinedDate).ThenByDescending(j => j.JobId).ToListAsync(ct);
        var types = jobs.Select(j => j.JobTypeId).Distinct().ToList();
        var rules = await db.JobTypeWorkingHourRules.AsNoTracking().Where(r => r.OrganisationId == org && types.Contains(r.JobTypeId) &&
            r.EffectiveFrom <= date.Date && (!r.EffectiveTo.HasValue || r.EffectiveTo >= date.Date)).ToListAsync(ct);
        Job? job = null; JobTypeWorkingHourRule? rule = null;
        foreach (var candidate in jobs)
        {
            var candidateRule = rules.SingleOrDefault(r => r.JobTypeId == candidate.JobTypeId);
            var boundary = date.Date.AddDays(-((7 + (int)date.DayOfWeek - (candidateRule?.WeekStartsOn ?? 0)) % 7));
            if (candidate.JoinedDate.Date >= boundary.AddDays(7) || candidate.TerminatedDate.HasValue && candidate.TerminatedDate.Value.Date < boundary) continue;
            job = candidate; rule = candidateRule; break;
        }
        var start = date.Date.AddDays(-((7 + (int)date.DayOfWeek - (rule?.WeekStartsOn ?? 0)) % 7));
        var end = start.AddDays(7);
        var result = new WeeklyWorkedHoursDto { WeekStart = start, WeekEnd = end.AddDays(-1), HasWorkingHoursRule = false,
            ConfigurationMessage = null };
        // Include the preceding day's shift so its hours after midnight are not lost at a week boundary.
        var facts = await AttendanceScheduleFacts.LoadAsync(db, person, start.AddDays(-1), end.AddDays(-1), org, ct);
        var now = DateTime.UtcNow.AddHours(5);
        (DateTime Start, DateTime End) Clip(DateTime a, DateTime b) => (a < start ? start : a, b > end ? end : b);
        var officialWorked = facts.Where(f => f.Purpose == "Official" && f.CheckIn.HasValue && f.CheckOut.HasValue && f.CheckOut <= now && f.CheckOut > f.CheckIn)
            .Select(f => Clip(f.CheckIn!.Value, f.CheckOut!.Value)).ToList();
        var dutyWorked = facts.Where(f => f.Purpose == "Duty" && f.CheckIn.HasValue && f.CheckOut.HasValue && f.CheckOut <= now && f.CheckOut > f.CheckIn)
            .Select(f => Clip(f.CheckIn!.Value, f.CheckOut!.Value)).ToList();
        var eligibility = await AttendanceEligibilityCalendar.LoadAsync(db, person, start, end.AddDays(-1), org, jobId: job?.JobId, ct: ct);
        officialWorked = officialWorked.Where(w => eligibility.GetValueOrDefault(w.Start.Date)?.HasApprovedPosition == true && eligibility[w.Start.Date].HasActiveSchedule).ToList();
        dutyWorked = dutyWorked.Where(w => eligibility.GetValueOrDefault(w.Start.Date)?.HasApprovedPosition == true).ToList();
        result.OfficialHours = WorkHourCoverage.Minutes(officialWorked) / 60m;
        result.DutyHours = WorkHourCoverage.Minutes(dutyWorked) / 60m;
        result.DutyOverlapHours = WorkHourCoverage.Calculate(officialWorked, dutyWorked).DutyWithinOfficialMinutes / 60m;
        var officialPlanned = facts.Where(f => f.Purpose == "Official" && f.RequiresAttendance)
            .Select(f => Clip(f.ScheduledStart, f.ScheduledEnd)).ToList();
        result.DutyOutsideOfficialHours = WorkHourCoverage.Calculate(officialPlanned.Concat(officialWorked), dutyWorked).PotentialOvertimeMinutes / 60m;
        result.DutyCountsTowardWeek = rule?.CountDutyHours == true;
        result.CurrentWeekHours = WorkHourCoverage.Minutes(rule?.CountDutyHours == true ? officialWorked.Concat(dutyWorked) : officialWorked) / 60m;
        if (job != null)
        {
            var leave = await db.Leaves.AsNoTracking().Where(l => l.JobId == job.JobId && l.LeaveStateId == 4 && l.FromDate < end && l.ToDate >= start)
                .Select(l => new { l.FromDate, l.ToDate }).ToListAsync(ct);
            var amendments = await OfficialScheduleAmendments.LoadAsync(db, org, start, end.AddDays(-1), ct);
            var effectiveSchedules = await EffectiveStaffSchedules.LoadAsync(db,job,start,end.AddDays(-1),ct);
            var requirements = await TemporaryWorkingHours.LoadAsync(db, org, start, end.AddDays(-1), ct);
            var dated = await db.WorkAssignments.AsNoTracking().Include(a => a.WorkPlan)
                .Where(a => a.IsValid && a.CancelledDate == null && a.WorkPlan.IsValid && a.WorkPlan.JobId == job.JobId &&
                    a.WorkPlan.OrganisationBusinessEntityId == org && a.SchedulePurpose == "Official" && a.WorkPlan.WorkDate >= start && a.WorkPlan.WorkDate < end).ToListAsync(ct);
            var personal = await db.LateCheckInRequests.AsNoTracking().Where(r => r.JobId == job.JobId && r.OrganisationId == org &&
                r.AppliedAtUtc != null && r.Request.RequestStateId == 4 && r.ScheduledStart >= start && r.ScheduledStart < end)
                .OrderBy(r => r.AppliedAtUtc).ToListAsync(ct);
            var rosterDays = Enumerable.Range(0, 7).Select(i => start.AddDays(i)).Where(day =>
                eligibility[day].HasApprovedPosition && eligibility[day].HasActiveSchedule && eligibility[day].CanBeAbsent &&
                day >= job.JoinedDate.Date && (!job.TerminatedDate.HasValue || day < job.TerminatedDate.Value.Date)).ToList();
            decimal required = 0, waived = 0, leaveCredit = 0;
            foreach (var day in rosterDays)
            {
                var intervals = new List<(DateTime Start, DateTime End)>();
                var alreadyWaivedStarts = new HashSet<TimeSpan>();
                foreach (var a in dated.Where(a => a.WorkPlan.WorkDate.Date == day))
                {
                    var change = personal.FirstOrDefault(r => r.WorkAssignmentId == a.WorkAssignmentId && r.ScheduledEnd.HasValue);
                    intervals.Add((change?.ScheduledStart ?? a.StartDateTime, change?.ScheduledEnd ?? a.EndDateTime));
                    if (change != null) alreadyWaivedStarts.Add(change.ScheduledStart.TimeOfDay);
                }
                if (intervals.Count == 0)
                {
                    var assignment = EffectiveStaffSchedules.On(effectiveSchedules,day).FirstOrDefault(c=>c.Assignment.SchedulePurpose=="Official")?.Assignment;
                    var templateId = assignment == null ? 0 : StaffScheduleCalendar.TemplateOn(assignment, day);
                    var template = templateId == 0 ? null : await db.WorkTemplates.AsNoTracking().SingleOrDefaultAsync(t => t.WorkTemplateId == templateId, ct);
                    var clock = assignment?.ScheduledStartTime ?? template?.DefaultStartTime;
                    if (clock.HasValue && template?.DefaultEndTime is TimeOnly finish)
                    {
                        var first = day.Add(clock.Value.ToTimeSpan()); var last = day.Add(finish.ToTimeSpan());
                        if (template.EndsNextDay || last <= first) last = last.AddDays(1);
                        intervals.Add((first, last));
                    }
                }
                decimal minutes = WorkHourCoverage.Minutes(intervals);
                if (minutes <= 0) continue;
                result.HasWorkingHoursRule = true;
                var waiver = amendments.Where(a => a.WorkDate.Date == day && (intervals.Any(i => i.Start.TimeOfDay == a.OriginalStart) || alreadyWaivedStarts.Contains(a.RevisedStart)))
                    .Select(a => (decimal)(a.RevisedStart - a.OriginalStart).TotalMinutes).DefaultIfEmpty(0).Max();
                if (amendments.Any(a => a.WorkDate.Date == day && alreadyWaivedStarts.Contains(a.RevisedStart))) minutes += waiver;
                var requirement = TemporaryWorkingHours.Select(requirements, org, job.JobTypeId, day);
                if (requirement != null) minutes = Math.Max(minutes, requirement.RequiredDailyMinutes);
                required += minutes;
                waiver = Math.Min(minutes, waiver);
                waived += waiver;
                if (leave.Any(l => l.FromDate.Date <= day && l.ToDate.Date >= day)) leaveCredit += minutes - waiver;
            }
            if (!result.HasWorkingHoursRule) result.ConfigurationMessage = "No active official schedule found for this week.";
            result.AmendmentCreditHours = Math.Round(waived, 0, MidpointRounding.AwayFromZero) / 60m;
            result.LeaveCreditHours = Math.Round(leaveCredit, 0, MidpointRounding.AwayFromZero) / 60m;
            result.RequiredWeekHours = Math.Round(Math.Max(0, required - waived - leaveCredit), 0, MidpointRounding.AwayFromZero) / 60m;
            var exceptionMinutes = await db.WeeklyHourExceptionRequests.AsNoTracking().Where(r => r.IndividualId == person &&
                r.OrganisationId == org && r.WeekStart == start && r.Request.RequestStateId == 4).SumAsync(r => (int?)r.ExcusedMinutes, ct) ?? 0;
            result.ApprovedExceptionHours = exceptionMinutes / 60m;
            // An exception excuses a shortfall; it must never manufacture overtime.
            result.ExceptionCreditHours = Math.Min(result.ApprovedExceptionHours, Math.Max(0, result.RequiredWeekHours - result.CurrentWeekHours));
            result.PotentialOvertimeHours = rosterDays.Count == 0 ? 0 : Math.Max(0, result.CurrentWeekHours - result.RequiredWeekHours);
            if (rule?.CountDutyHours != true && rosterDays.Count > 0) result.PotentialOvertimeHours += result.DutyOutsideOfficialHours;
            result.RequiredWeekHours -= result.ExceptionCreditHours;
            result.RemainingWeekHours = Math.Max(0, result.RequiredWeekHours - result.CurrentWeekHours);
        }
        var makeup = await WeeklyAttendanceMakeup.LoadAsync(db, person, org, start, end, ct, rule?.CountDutyHours == true);
        result.ApprovedEarlyCheckoutShortfallHours = makeup.ShortfallHours;
        result.MakeupCompletedHours = makeup.CompletedHours; result.MakeupRemainingHours = Math.Max(0, makeup.RemainingHours - result.ExceptionCreditHours);
        return result;
    }
}
