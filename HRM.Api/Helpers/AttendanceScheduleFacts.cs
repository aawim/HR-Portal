using HRM.DTOs.Attendance;
using HRM.Models;
using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;

namespace HRM.Helpers;

public static class AttendanceScheduleFacts
{
    public static async Task<List<StaffScheduleAttendanceDto>> LoadAsync(HrmTeContext db, int person, DateTime from, DateTime to, int? org, CancellationToken ct = default, List<AttendanceLog>? sourceLogs = null)
    {
        if (db.Model.FindEntityType(typeof(WorkAssignment)) == null) return [];
        var end = to.Date.AddDays(1);
        var assignments = await db.WorkAssignments.AsNoTracking().Include(a => a.WorkPlan).ThenInclude(p => p.WorkPlanSegments).Include(a => a.WorkTemplateType)
            .Include(a => a.WorkAssignmentSegments).ThenInclude(s => s.WorkSegmentType)
            .Where(a => a.IsValid && a.CancelledDate == null && a.WorkPlan.IsValid &&
                (!org.HasValue || a.WorkPlan.OrganisationBusinessEntityId == org) &&
                a.WorkPlan.WorkDate >= from.Date && a.WorkPlan.WorkDate < end &&
                (a.WorkAssignmentOwners.Any(o => o.IndividualId == person && o.IsValid && o.IsCurrentOwner &&
                    o.EffectiveFrom <= a.StartDateTime && (o.EffectiveTo == null || o.EffectiveTo >= a.StartDateTime)) ||
                 a.WorkPlan.IndividualId == person && !a.WorkAssignmentOwners.Any(o => o.IsValid && o.IsCurrentOwner && o.IndividualId != person)))
            .ToListAsync(ct);

        var snapshotOwners = assignments.SelectMany(a => a.WorkPlan.WorkPlanSegments.Where(p => p.IsValid && p.SchedulePurpose == a.SchedulePurpose &&
            a.WorkAssignmentSegments.Any(s => s.IsValid && s.WorkTemplateSegmentId == p.WorkTemplateSegmentId && s.WorkSegmentTypeId == p.WorkSegmentTypeId && s.StartDateTime == p.StartDateTime))
            .Select(p => new { p.WorkPlanSegmentId, a.WorkAssignmentId }))
            .GroupBy(x => x.WorkPlanSegmentId).Where(g => g.Select(x => x.WorkAssignmentId).Distinct().Count() == 1)
            .ToDictionary(g => g.Key, g => g.First().WorkAssignmentId);

        var logs = sourceLogs ?? await db.AttendanceLogs.AsNoTracking().Include(l => l.AttendanceLogResolutions)
            .Where(l => l.IndividualId == person && (!org.HasValue || l.OrganisationId == org) &&
                l.AttendanceLogStateId == HRM.Components.Shared.SharedConfig.AttendanceLogStates.Valid && l.Date >= from.Date && l.Date < end.AddDays(1))
            .ToListAsync(ct);

        var amendments = await OfficialScheduleAmendments.LoadAsync(db, org, from, to, ct);
        var adjustments = await TemporaryWorkingHours.AdjustmentsAsync(db, assignments, ct);

        return assignments.Select(a =>
        {
            var samePurposeCountOnPlan = assignments.Count(x => x.WorkPlanId == a.WorkPlanId && x.SchedulePurpose == a.SchedulePurpose);
            var events = logs.Select(l => new
            {
                l.Date,
                l.AttendanceLogId,
                TotalValidResolutionsOnLog = l.AttendanceLogResolutions.Count(r => r.IsValid && r.AttendanceResolutionStatusId == HRM.Constants.AttendanceResolutionStatusIds.Resolved),
                Resolution = l.AttendanceLogResolutions
                    .Where(r => r.IsValid &&
                        ((r.WorkAssignmentId ?? snapshotOwners.GetValueOrDefault(r.WorkPlanSegmentId ?? 0)) == a.WorkAssignmentId ||
                         (samePurposeCountOnPlan == 1 && r.WorkPlanId == a.WorkPlanId && r.SchedulePurpose == a.SchedulePurpose)))
                    .OrderByDescending(r => r.AttendanceResolutionStatusId == HRM.Constants.AttendanceResolutionStatusIds.Resolved)
                    .ThenByDescending(r => r.AttendanceLogResolutionId)
                    .FirstOrDefault()
            }).Where(e => e.Resolution != null &&
                (e.Resolution.AttendanceResolutionStatusId == HRM.Constants.AttendanceResolutionStatusIds.Resolved ||
                 e.Resolution.AttendanceResolutionStatusId == HRM.Constants.AttendanceResolutionStatusIds.DuplicatePunch))
            .ToList();

            var start = a.WorkAssignmentSegments.Where(s => s.IsValid && (s.ExpectedInOutModeId == 1 || s.WorkSegmentType.Code == "CHECK_IN" || s.WorkSegmentType.Code == "DUTY_CHECK_IN"))
                .Select(s => s.StartDateTime).DefaultIfEmpty(a.StartDateTime).Min();
            start = OfficialScheduleAmendments.Start(amendments, a.WorkPlan.OrganisationBusinessEntityId, a.SchedulePurpose, start);

            var finish = a.WorkAssignmentSegments.Where(s => s.IsValid && (s.ExpectedInOutModeId == 2 || s.WorkSegmentType.Code == "CHECK_OUT" || s.WorkSegmentType.Code == "DUTY_CHECK_OUT"))
                .Select(s => s.StartDateTime).DefaultIfEmpty(a.EndDateTime).Max();
            if (adjustments.TryGetValue(a.WorkAssignmentId, out var adjustment) && !adjustment.HasEarlyCheckout) finish = finish.AddMinutes(adjustment.DeltaMinutes);

            var type = (a.WorkTemplateType?.Name ?? "") + " " + (a.WorkTemplateType?.Code ?? "");
            var shift = a.SchedulePurpose.Equals("Duty", StringComparison.OrdinalIgnoreCase) ||
                type.Contains("shift", StringComparison.OrdinalIgnoreCase) || type.Contains("rotat", StringComparison.OrdinalIgnoreCase);

            var checkInEvent = AttendanceTiming.Closest(
                events.Where(e => e.Resolution!.AttendanceClockTypeId == 1), e => e.Date, start);
            var checkOutEvent = events.Where(e => e.Resolution!.AttendanceClockTypeId == 2 && e.Resolution.LogicalEventType != "OfficialBreakOut")
                .OrderByDescending(e => e.Date).FirstOrDefault();
            var breakOutEvent = events.Where(e => e.Resolution!.AttendanceClockTypeId == (int)HRM.Enum.AttendanceClockType.BreakOut || e.Resolution.LogicalEventType == "OfficialBreakOut")
                .OrderBy(e => e.Date).FirstOrDefault();
            var breakInEvent = events.Where(e => e.Resolution!.AttendanceClockTypeId == (int)HRM.Enum.AttendanceClockType.BreakIn || e.Resolution.LogicalEventType == "OfficialBreakIn")
                .OrderByDescending(e => e.Date).FirstOrDefault();

            var plannedMinutes = Math.Max(0, (int)Math.Round((finish - start).TotalMinutes));
            int actualMinutes = 0;
            int creditedMinutes = 0;
            int lateMinutes = 0;
            int earlyMinutes = 0;
            string state = "NotStarted";
            string? reason = null;

            if (checkInEvent != null)
            {
                lateMinutes = AttendanceTiming.CalculateLateMinutes(start, checkInEvent.Date, a.SchedulePurpose == "Duty" ? a.GraceMinutes : 0);
            }

            if (checkInEvent != null && checkOutEvent != null && checkOutEvent.Date > checkInEvent.Date)
            {
                actualMinutes = (int)Math.Round((checkOutEvent.Date - checkInEvent.Date).TotalMinutes);
                earlyMinutes = AttendanceTiming.CalculateEarlyDepartureMinutes(finish, checkOutEvent.Date);
                var clampedStart = checkInEvent.Date < start ? start : checkInEvent.Date;
                var clampedEnd = checkOutEvent.Date > finish ? finish : checkOutEvent.Date;
                if (clampedEnd > clampedStart)
                    creditedMinutes = (int)Math.Round((clampedEnd - clampedStart).TotalMinutes);
                state = "Completed";
                reason = checkOutEvent.Date > finish && a.SchedulePurpose == "Duty"
                    ? $"Delayed duty check-out at {checkOutEvent.Date:HH:mm}; credited duration clamped to {finish:HH:mm} ({creditedMinutes}m)."
                    : $"Completed ({checkInEvent.Date:HH:mm}-{checkOutEvent.Date:HH:mm}).";
            }
            else if (checkInEvent != null && checkOutEvent == null)
            {
                bool pastEnd = DateTime.UtcNow.AddHours(5) > finish.AddMinutes(60) || a.WorkPlan.WorkDate.Date < DateTime.UtcNow.AddHours(5).Date;
                state = pastEnd ? "MissingClockOut" : breakOutEvent != null && breakInEvent == null ? "OnBreak" : "ClockedIn";
                reason = pastEnd
                    ? $"Check-in recorded at {checkInEvent.Date:HH:mm}; check-out at {finish:HH:mm} is missing."
                    : $"Clocked in at {checkInEvent.Date:HH:mm}.";
            }
            else if (checkInEvent == null && checkOutEvent != null)
            {
                state = "MissingClockIn";
                reason = $"Check-out recorded at {checkOutEvent.Date:HH:mm}; check-in at {start:HH:mm} is missing.";
            }

            return new StaffScheduleAttendanceDto(a.WorkPlan.WorkDate.Date, a.Name, a.SchedulePurpose, shift,
                a.RequiresAttendance || a.WorkAssignmentSegments.Any(s => s.IsValid && s.IsMandatory && s.RequiresAttendance),
                start, finish, checkInEvent?.Date, checkOutEvent?.Date)
            {
                WorkAssignmentId = a.WorkAssignmentId,
                CheckInLogId = checkInEvent?.AttendanceLogId,
                CheckOutLogId = checkOutEvent?.AttendanceLogId,
                BreakOut = breakOutEvent?.Date,
                BreakIn = breakInEvent?.Date,
                PlannedMinutes = plannedMinutes,
                ActualPresenceMinutes = actualMinutes,
                CreditedWorkedMinutes = creditedMinutes,
                LateMinutes = lateMinutes,
                EarlyDepartureMinutes = earlyMinutes,
                ObligationState = state,
                StatusReason = reason,
                SharesCheckInEvent = checkInEvent is { TotalValidResolutionsOnLog: > 1 },
                SharesCheckOutEvent = checkOutEvent is { TotalValidResolutionsOnLog: > 1 }
            };
        }).OrderBy(a => a.Date).ThenBy(a => a.Purpose == "Official" ? 0 : 1).ThenBy(a => a.ScheduledStart).ToList();
    }
}
