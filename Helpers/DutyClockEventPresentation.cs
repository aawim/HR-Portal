using HRM.DTOs.Attendance;
using HRM.Enum;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Helpers;

public static class DutyClockEventPresentation
{
    // Reconstruct display labels from dated schedules; physical punches and audit history stay intact.
    public static async Task ApplyAsync(HrmTeContext db, List<AttendanceRawLogDto> logs, int person, int job, int org,
        DateTime date, CancellationToken ct = default)
    {
        if (logs.Count == 0 || db.Model.FindEntityType(typeof(HRM.Models.WorkPlanning.WorkAssignment)) == null) return;
        var assignments = await db.WorkAssignments.AsNoTracking().Include(a => a.WorkAssignmentSegments).ThenInclude(s => s.WorkSegmentType)
            .Include(a => a.WorkPlan).ThenInclude(p => p.WorkPlanSegments)
            .Where(a => a.IsValid && a.CancelledDate == null && a.WorkPlan.IsValid &&
                a.WorkPlan.JobId == job && a.WorkPlan.OrganisationBusinessEntityId == org && a.WorkPlan.IndividualId == person &&
                a.WorkPlan.WorkDate >= date.Date.AddDays(-1) && a.WorkPlan.WorkDate <= date.Date).ToListAsync(ct);
        var adjustments = await TemporaryWorkingHours.AdjustmentsAsync(db, assignments, ct);
        var amendments = await OfficialScheduleAmendments.LoadAsync(db, org, date.AddDays(-1), date, ct);
        var boundaries = assignments.SelectMany(a => a.WorkAssignmentSegments.Where(s => s.IsValid && s.RequiresAttendance &&
                (s.ExpectedInOutModeId is 1 or 2 || s.WorkSegmentType.Code is "CHECK_IN" or "DUTY_CHECK_IN" or "CHECK_OUT" or "DUTY_CHECK_OUT"))
            .Select(s => {
                var timing = (Start: s.StartDateTime, End: s.EndDateTime ?? s.StartDateTime);
                if (adjustments.TryGetValue(a.WorkAssignmentId, out var adjustment)) timing = TemporaryWorkingHours.Segment(adjustment, s.WorkSegmentType.Code, timing.Start, timing.End);
                var amended = OfficialScheduleAmendments.Segment(amendments, org, a.SchedulePurpose, s.WorkSegmentType.Code, timing.Start, timing.End, s.GraceBeforeMinutes);
                var clock = s.ExpectedInOutModeId == 2 || s.WorkSegmentType.Code is "CHECK_OUT" or "DUTY_CHECK_OUT" ? AttendanceClockType.CheckOut : AttendanceClockType.CheckIn;
                return new { Assignment = a, Segment = s, Start = amended.Start, End = amended.End, Clock = clock, GraceBefore = amended.GraceBefore };
            })).ToList();
        var occupied = new HashSet<long>();
        foreach (var log in logs.OrderBy(l => l.LogDateTime))
        {
            var owners = assignments.Where(a => log.WorkAssignmentId == a.WorkAssignmentId || log.WorkPlanSegmentId.HasValue && a.WorkPlan.WorkPlanSegments.Any(p =>
                p.WorkPlanSegmentId == log.WorkPlanSegmentId && p.IsValid && p.SchedulePurpose == a.SchedulePurpose &&
                a.WorkAssignmentSegments.Any(s => s.IsValid && s.WorkTemplateSegmentId == p.WorkTemplateSegmentId && s.WorkSegmentTypeId == p.WorkSegmentTypeId && s.StartDateTime == p.StartDateTime))).ToList();
            if (owners.Count == 1)
            {
                var owner = owners[0];
                log.WorkAssignmentId = owner.WorkAssignmentId; log.SchedulePurpose = owner.SchedulePurpose; log.ScheduleName = owner.Name;
                log.ScheduledStart = boundaries.Where(b => b.Assignment.WorkAssignmentId == owner.WorkAssignmentId && b.Clock == AttendanceClockType.CheckIn).Select(b => b.Start).DefaultIfEmpty(owner.StartDateTime).Min();
                log.ScheduledEnd = boundaries.Where(b => b.Assignment.WorkAssignmentId == owner.WorkAssignmentId && b.Clock == AttendanceClockType.CheckOut).Select(b => b.Start).DefaultIfEmpty(owner.EndDateTime).Max();
            }
            var linked = boundaries.Where(b => log.WorkPlanSegmentId.HasValue && b.Assignment.WorkPlan.WorkPlanSegments.Any(p =>
                p.WorkPlanSegmentId == log.WorkPlanSegmentId && p.IsValid && p.SchedulePurpose == b.Assignment.SchedulePurpose &&
                p.WorkTemplateSegmentId == b.Segment.WorkTemplateSegmentId && p.WorkSegmentTypeId == b.Segment.WorkSegmentTypeId &&
                p.StartDateTime == b.Segment.StartDateTime)).ToList();
            var match = linked.Count == 1 ? linked[0] : null;
            if ((log.ClockType == AttendanceClockType.Unresolved || log.ClockType == AttendanceClockType.CheckIn && match != null && log.LogDateTime < match.Start) && log.InOutModeId != 2)
            {
                var candidates = boundaries.Where(b => b.Clock == AttendanceClockType.CheckIn && !occupied.Contains(b.Assignment.WorkAssignmentId) &&
                    log.LogDateTime >= b.Start.AddMinutes(-180) && log.LogDateTime < boundaries.Where(e => e.Assignment.WorkAssignmentId == b.Assignment.WorkAssignmentId && e.Clock == AttendanceClockType.CheckOut && e.Start > b.Start)
                        .Select(e => e.Start).DefaultIfEmpty(b.End).Min())
                    .OrderBy(b => Math.Abs((log.LogDateTime - b.Start).TotalMinutes)).ToList();
                if (candidates.Count == 1 || candidates.Count > 1 && Math.Abs((log.LogDateTime - candidates[0].Start).TotalMinutes) < Math.Abs((log.LogDateTime - candidates[1].Start).TotalMinutes))
                    match = candidates[0];
            }
            if (match == null || log.ClockType == AttendanceClockType.Ignored) continue;
            log.WorkAssignmentId = match.Assignment.WorkAssignmentId;
            log.ScheduleName = match.Assignment.Name;
            log.ScheduledStart = boundaries.Where(b => b.Assignment.WorkAssignmentId == match.Assignment.WorkAssignmentId && b.Clock == AttendanceClockType.CheckIn).Select(b => b.Start).DefaultIfEmpty(match.Assignment.StartDateTime).Min();
            log.ScheduledEnd = boundaries.Where(b => b.Assignment.WorkAssignmentId == match.Assignment.WorkAssignmentId && b.Clock == AttendanceClockType.CheckOut).Select(b => b.Start).DefaultIfEmpty(match.Assignment.EndDateTime).Max();
            log.SchedulePurpose = match.Assignment.SchedulePurpose;
            log.ClockType = match.Clock;
            log.SegmentName = $"{match.Assignment.SchedulePurpose} · {(match.Clock == AttendanceClockType.CheckIn ? "Check in" : "Check out")}";
            log.ResolutionMessage = $"{match.Assignment.SchedulePurpose} {(match.Clock == AttendanceClockType.CheckIn ? "check-in" : "check-out")} matched to {match.Assignment.Name} at {match.Start:HH:mm}.";
            if (match.Clock != AttendanceClockType.CheckIn) continue;
            occupied.Add(match.Assignment.WorkAssignmentId);
            log.LateMinutes = log.LogDateTime > match.Start.AddMinutes(match.Segment.GraceAfterMinutes)
                ? (int)Math.Ceiling((log.LogDateTime - match.Start).TotalMinutes) : 0;
        }
    }
}
