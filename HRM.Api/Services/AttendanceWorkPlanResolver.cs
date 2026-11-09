using HRM.Constants;
using HRM.DTOs.Attendance;
using HRM.Enum;
using HRM.Models;
using HRM.Services.Attendance.AttendancePlan;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public sealed class AttendanceWorkPlanResolver : IAttendanceWorkPlanResolver
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
 
        private readonly IWorkPlanGenerator _workPlanGenerator;

        private const int EarlyCheckInResolutionMinutes = 180;

        // A punch after a scheduled checkout is still that checkout (for example a
        // duty ending at 12:00 punched at 12:20). It must never be reinterpreted as
        // an official check-in just because the official check-in window is wide.
        private const int LateCheckOutResolutionMinutes = 180;

        // A checkout may be punched slightly early (13:55 for a 14:00 checkout).
        // It is deliberately short so a punch in the middle of a duty is not
        // turned into an early check-out of the following boundary.
        private const int EarlyCheckOutResolutionMinutes = 30;


        public AttendanceWorkPlanResolver(
            IDbContextFactory<HrmTeContext> dbFactory, 
            IWorkPlanGenerator workPlanGenerator
            )
        {
            _dbFactory = dbFactory;
            _workPlanGenerator = workPlanGenerator;
        }


        public async Task<AttendancePlanResolutionResult> ResolveAsync(
            int individualId,
            int jobId,
            int organisationBusinessEntityId,
            DateTime clockTime,
            CancellationToken cancellationToken = default)
        {
            var workDate =
                DateOnly.FromDateTime(clockTime);

            var generatedPlan =
                await _workPlanGenerator.GenerateOrGetAsync(
                    individualId,
                    jobId,
                    organisationBusinessEntityId,
                    workDate,
                    cancellationToken);

            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            long? selectedPlanId = generatedPlan?.WorkPlanId;
            // Overnight shifts belong to the day they started, even when the
            // checkout is on a cycle rest day or a different calendar day.
            var previousDate = clockTime.Date.AddDays(-1);
            var overnightBoundaries = await db.WorkPlanSegments.AsNoTracking()
                .Where(x => x.IsValid && x.RequiresAttendance && x.WorkPlan.IsValid &&
                    x.WorkPlan.IndividualId == individualId && x.WorkPlan.JobId == jobId &&
                    x.WorkPlan.OrganisationBusinessEntityId == organisationBusinessEntityId &&
                    x.WorkPlan.WorkDate == previousDate && x.StartDateTime >= clockTime.Date &&
                    (x.WorkSegmentType.Code == "CHECK_OUT" || x.WorkSegmentType.Code == "DUTY_CHECK_OUT"))
                .Select(x => new { x.WorkPlanId, x.StartDateTime, x.EndDateTime, x.GraceBeforeMinutes }).ToListAsync(cancellationToken);
            var overnightBoundary = overnightBoundaries.Where(x => clockTime >= x.StartDateTime.AddMinutes(-x.GraceBeforeMinutes) && clockTime <= x.EndDateTime)
                .OrderBy(x => Math.Abs((clockTime - x.StartDateTime).TotalMinutes)).FirstOrDefault();
            if (overnightBoundary != null)
            {
                var currentSegments = await db.WorkPlanSegments.AsNoTracking().Where(x => x.IsValid && x.RequiresAttendance && x.WorkPlanId == selectedPlanId)
                    .Select(x => new { x.StartDateTime, x.EndDateTime, x.GraceBeforeMinutes, Code = x.WorkSegmentType.Code }).ToListAsync(cancellationToken);
                var nearestCurrent = currentSegments.Where(x =>
                    (RequiresCheckIn(x.Code) && clockTime >= x.StartDateTime.AddMinutes(-EarlyCheckInResolutionMinutes) && clockTime <= x.EndDateTime) ||
                    (RequiresCheckOut(x.Code) && clockTime >= x.StartDateTime.AddMinutes(-x.GraceBeforeMinutes) && clockTime <= x.EndDateTime))
                    .Select(x => Math.Abs((clockTime - x.StartDateTime).TotalMinutes)).DefaultIfEmpty(double.MaxValue).Min();
                if (Math.Abs((clockTime - overnightBoundary.StartDateTime).TotalMinutes) <= nearestCurrent) selectedPlanId = overnightBoundary.WorkPlanId;
            }
            if (!selectedPlanId.HasValue)
            {
                return new AttendancePlanResolutionResult
                {
                    JobId = jobId,
                    State = AttendancePlanResolutionState.NoWorkPlan,
                    ClockType = AttendanceClockType.Unresolved,
                    Message = "No applicable work plan or work-template assignment was found for this attendance event."
                };
            }

            var workPlanId = selectedPlanId.Value;

            var amendmentRows = await HRM.Helpers.OfficialScheduleAmendments.LoadAsync(db, organisationBusinessEntityId,
                clockTime.Date.AddDays(-1), clockTime.Date, cancellationToken);
            var teamAdjustment = await HRM.Helpers.TeamScheduleAdjustments.FindAsync(db, individualId, organisationBusinessEntityId,
                (await db.WorkPlans.Where(x => x.WorkPlanId == workPlanId).Select(x => x.WorkDate).SingleAsync(cancellationToken)), cancellationToken);
            var segmentEntities = await db.WorkPlanSegments.AsNoTracking().Include(x => x.WorkSegmentType)
                .Where(x => x.WorkPlanId == workPlanId && x.IsValid).OrderBy(x => x.SequenceNumber)
                .Select(x => new { x.WorkPlanSegmentId, x.Name, x.StartDateTime, x.EndDateTime, x.SequenceNumber,
                    x.SchedulePurpose, TemplateId = x.WorkTemplateSegment == null ? (int?)null : x.WorkTemplateSegment.WorkTemplateId,
                    WorkSegmentTypeCode = x.WorkSegmentType.Code, x.RequiresAttendance, x.IsMandatory, x.GraceBeforeMinutes, x.GraceAfterMinutes })
                .ToListAsync(cancellationToken);
            var hourAssignments = await db.WorkAssignments.AsNoTracking().Include(a => a.WorkPlan).Where(a => a.WorkPlanId == workPlanId && a.IsValid).ToListAsync(cancellationToken);
            var hourAdjustments = await HRM.Helpers.TemporaryWorkingHours.AdjustmentsAsync(db, hourAssignments, cancellationToken);
            var segments = segmentEntities.Select(x =>
            {
                var adjusted = (Start: x.StartDateTime, End: x.EndDateTime);
                var matches = hourAdjustments.Values.Where(a => x.SchedulePurpose == "Official" && (a.TemplateId == x.TemplateId || hourAdjustments.Count == 1)).ToList();
                if (matches.Count == 1) adjusted = HRM.Helpers.TemporaryWorkingHours.Segment(matches[0], x.WorkSegmentTypeCode, adjusted.Start, adjusted.End);
                var timing = HRM.Helpers.OfficialScheduleAmendments.Segment(amendmentRows, organisationBusinessEntityId,
                    x.SchedulePurpose, x.WorkSegmentTypeCode, adjusted.Start, adjusted.End, x.GraceBeforeMinutes);
                timing = HRM.Helpers.TeamScheduleAdjustments.Segment(teamAdjustment, x.SchedulePurpose, x.WorkSegmentTypeCode,
                    timing.Start, timing.End, timing.GraceBefore);
                return new { x.WorkPlanSegmentId, x.Name, StartDateTime = timing.Start, EndDateTime = timing.End,
                    x.SequenceNumber, x.TemplateId, x.SchedulePurpose, x.WorkSegmentTypeCode, x.RequiresAttendance, x.IsMandatory,
                    GraceBeforeMinutes = timing.GraceBefore, x.GraceAfterMinutes };
            }).ToList();

            if (segments.Count == 0)
            {
                return new AttendancePlanResolutionResult
                {
                    WorkPlanId = workPlanId,
                    JobId = jobId,
                    State = AttendancePlanResolutionState.NoSegment,
                    ClockType = AttendanceClockType.Unresolved,
                    Message = "The work plan does not contain any valid segments."
                };
            }

            var pairedBreakSegment =
                segments
                    .Where(x =>
                        x.RequiresAttendance && IsPairedBreakSegment(x.WorkSegmentTypeCode))
                    .Where(x =>
                        clockTime >= x.StartDateTime &&
                        clockTime <= x.EndDateTime)
                    .OrderBy(x => x.SequenceNumber)
                    .FirstOrDefault();

            // ============================================================
            // OPEN CHECK-IN STATE PER SCHEDULE
            // ============================================================

            var scheduleGroupKeys = segments.ToDictionary(
                x => x.WorkPlanSegmentId,
                x => ScheduleGroupKey(x.SchedulePurpose, x.TemplateId));

            var openScheduleGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var priorResolutions = await db.AttendanceLogResolutions
                .AsNoTracking()
                .Where(x =>
                    x.WorkPlanId == workPlanId &&
                    x.IsValid &&
                    x.WorkPlanSegmentId.HasValue &&
                    x.AttendanceLog.Date < clockTime &&
                    x.AttendanceClockTypeId.HasValue &&
                    (
                        x.AttendanceClockTypeId.Value == (int)AttendanceClockType.CheckIn ||
                        x.AttendanceClockTypeId.Value == (int)AttendanceClockType.CheckOut
                    ))
                .OrderByDescending(x => x.AttendanceLog.Date)
                .Select(x => new
                {
                    WorkPlanSegmentId = x.WorkPlanSegmentId!.Value,
                    ClockTypeId = x.AttendanceClockTypeId!.Value
                })
                .ToListAsync(cancellationToken);

            var settledScheduleGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var prior in priorResolutions)
            {
                if (!scheduleGroupKeys.TryGetValue(prior.WorkPlanSegmentId, out var groupKey))
                    continue;

                if (!settledScheduleGroups.Add(groupKey))
                    continue;

                if (prior.ClockTypeId == (int)AttendanceClockType.CheckIn)
                    openScheduleGroups.Add(groupKey);
            }

            // ============================================================
            // LUNCH BREAK
            //
            // Explicit shift/duty clock boundaries OR an open duty awaiting
            // check-out take precedence over a standalone break return, while
            // still allowing the break to be attached as a shared association!
            // ============================================================
            var hasOpenDutyAwaitingExit = segments.Any(x =>
                x.SchedulePurpose == "Duty" &&
                RequiresCheckOut(x.WorkSegmentTypeCode) &&
                openScheduleGroups.Contains(scheduleGroupKeys[x.WorkPlanSegmentId]) &&
                clockTime >= x.StartDateTime.AddMinutes(-EarlyCheckOutResolutionMinutes) &&
                clockTime <= x.EndDateTime.AddMinutes(LateCheckOutResolutionMinutes));

            var hasExplicitBoundary = hasOpenDutyAwaitingExit || segments.Any(x => x.RequiresAttendance &&
                (RequiresCheckIn(x.WorkSegmentTypeCode) || RequiresCheckOut(x.WorkSegmentTypeCode)) &&
                clockTime >= x.StartDateTime.AddMinutes(-x.GraceBeforeMinutes) && clockTime <= x.EndDateTime);

            if (pairedBreakSegment != null && !hasExplicitBoundary)
            {
                var scheduleSegmentIds = segments.Where(x => x.TemplateId == pairedBreakSegment.TemplateId).Select(x => x.WorkPlanSegmentId).ToList();
                var lastAttendanceState =
                    await db.AttendanceLogResolutions
                        .AsNoTracking()
                        .Where(x =>
                            x.WorkPlanId == workPlanId &&
                            x.IsValid &&
                            x.AttendanceLog.Date <= clockTime &&
                            x.WorkPlanSegmentId.HasValue && scheduleSegmentIds.Contains(x.WorkPlanSegmentId.Value) &&
                            x.AttendanceClockTypeId.HasValue &&
                            (
                                x.AttendanceClockTypeId.Value ==
                                    (int)AttendanceClockType.CheckIn
                                ||
                                x.AttendanceClockTypeId.Value ==
                                    (int)AttendanceClockType.CheckOut
                            ))
                        .OrderByDescending(x =>
                            x.AttendanceLog.Date)
                        .Select(x =>
                            (AttendanceClockType?)
                                x.AttendanceClockTypeId!.Value)
                        .FirstOrDefaultAsync(cancellationToken);

                if (!lastAttendanceState.HasValue)
                {
                    return new AttendancePlanResolutionResult
                    {
                        WorkPlanId = workPlanId,
                        WorkPlanSegmentId = pairedBreakSegment.WorkPlanSegmentId,
                        JobId = jobId,
                        SchedulePurpose = "Official",
                        SegmentName = pairedBreakSegment.Name,
                        ClockType = AttendanceClockType.Unresolved,
                        State = AttendancePlanResolutionState.Unresolved,
                        BoundaryTime = clockTime,
                        DistanceMinutes = 0,
                        Message =
                            $"Cannot determine attendance direction for '{pairedBreakSegment.Name}' because no previous CheckIn or CheckOut exists."
                    };
                }

                var lunchClockType = lastAttendanceState.Value == AttendanceClockType.CheckIn
                    ? AttendanceClockType.CheckOut
                    : AttendanceClockType.CheckIn;

                return new AttendancePlanResolutionResult
                {
                    WorkPlanId = workPlanId,
                    WorkPlanSegmentId = pairedBreakSegment.WorkPlanSegmentId,
                    JobId = jobId,
                    SchedulePurpose = "Official",
                    SegmentName = pairedBreakSegment.Name,
                    ClockType = lunchClockType,
                    LogicalEventType = lunchClockType == AttendanceClockType.CheckOut ? "OfficialBreakOut" : "OfficialBreakIn",
                    MatchMethod = "OfficialBreakWindow",
                    MatchReason = lunchClockType == AttendanceClockType.CheckOut
                        ? $"Checked out for '{pairedBreakSegment.Name}'."
                        : $"Checked in from '{pairedBreakSegment.Name}'.",
                    State = AttendancePlanResolutionState.Resolved,
                    BoundaryTime = clockTime,
                    DistanceMinutes = 0,
                    Message = lunchClockType == AttendanceClockType.CheckOut
                        ? $"Checked out for '{pairedBreakSegment.Name}'."
                        : $"Checked in from '{pairedBreakSegment.Name}'."
                };
            }

            var candidates = new List<BoundaryCandidate>();

            foreach (var segment in segments.Where(x => x.RequiresAttendance))
            {
                var code = segment.WorkSegmentTypeCode;

                if (RequiresCheckIn(code))
                {
                    var expectedTime = segment.StartDateTime;
                    var windowStart = expectedTime.AddMinutes(-EarlyCheckInResolutionMinutes);
                    var exactEnd = segment.EndDateTime;
                    if (exactEnd < expectedTime) exactEnd = expectedTime;
                    var extendedEnd = exactEnd;
                    if (segment.SchedulePurpose is "Duty" or "Official")
                    {
                        var dutyEnd = segments.Where(s => s.SchedulePurpose == segment.SchedulePurpose && s.TemplateId == segment.TemplateId &&
                            RequiresCheckOut(s.WorkSegmentTypeCode) && s.StartDateTime > expectedTime)
                            .Select(s => (DateTime?)s.StartDateTime).Min();
                        if (dutyEnd.HasValue && dutyEnd.Value.AddTicks(-1) > extendedEnd) extendedEnd = dutyEnd.Value.AddTicks(-1);
                    }

                    if (clockTime >= windowStart && clockTime <= extendedEnd)
                    {
                        candidates.Add(
                            new BoundaryCandidate
                            {
                                WorkPlanSegmentId = segment.WorkPlanSegmentId,
                                SegmentName = segment.Name,
                                SchedulePurpose = segment.SchedulePurpose,
                                IsMandatory = segment.IsMandatory,
                                GroupKey = scheduleGroupKeys[segment.WorkPlanSegmentId],
                                HasOpenCheckIn = openScheduleGroups.Contains(scheduleGroupKeys[segment.WorkPlanSegmentId]),
                                ClockType = AttendanceClockType.CheckIn,
                                BoundaryTime = expectedTime,
                                WindowStart = windowStart,
                                WindowEnd = exactEnd,
                                ExtendedWindowEnd = extendedEnd,
                                DistanceMinutes = Math.Abs((clockTime - expectedTime).TotalMinutes)
                            });
                    }
                }

                if (RequiresCheckOut(code))
                {
                    var expectedTime = segment.StartDateTime;
                    var windowStart = segment.StartDateTime.AddMinutes(-segment.GraceBeforeMinutes);
                    var windowEnd = segment.EndDateTime;
                    if (windowEnd < expectedTime) windowEnd = expectedTime;

                    if (clockTime >= windowStart.AddMinutes(-EarlyCheckOutResolutionMinutes) &&
                        clockTime <= windowEnd.AddMinutes(LateCheckOutResolutionMinutes))
                    {
                        candidates.Add(
                            new BoundaryCandidate
                            {
                                WorkPlanSegmentId = segment.WorkPlanSegmentId,
                                SegmentName = segment.Name,
                                SchedulePurpose = segment.SchedulePurpose,
                                IsMandatory = segment.IsMandatory,
                                GroupKey = scheduleGroupKeys[segment.WorkPlanSegmentId],
                                HasOpenCheckIn = openScheduleGroups.Contains(scheduleGroupKeys[segment.WorkPlanSegmentId]),
                                ClockType = AttendanceClockType.CheckOut,
                                BoundaryTime = expectedTime,
                                WindowStart = windowStart,
                                WindowEnd = windowEnd,
                                DistanceMinutes = Math.Abs((clockTime - expectedTime).TotalMinutes)
                            });
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return new AttendancePlanResolutionResult
                {
                    WorkPlanId = workPlanId,
                    JobId = jobId,
                    State = AttendancePlanResolutionState.OutsideResolutionWindow,
                    ClockType = AttendanceClockType.Unresolved,
                    BoundaryTime = clockTime,
                    DistanceMinutes = 0,
                    Message = $"Attendance event at {clockTime:HH:mm} does not fall inside an applicable attendance window."
                };
            }

            var bestCandidate = SelectBestCandidate(candidates, clockTime);

            // Coincident check-outs close both schedules with one physical punch.
            // Coincident check-ins are allocated in sequence instead: the first
            // punch satisfies the official schedule, the next punch satisfies duty.
            var secondaryCandidate = bestCandidate.SchedulePurpose == "Official" &&
                bestCandidate.ClockType == AttendanceClockType.CheckOut
                ? candidates.FirstOrDefault(x =>
                    x.SchedulePurpose == "Duty" &&
                    x.ClockType == AttendanceClockType.CheckOut &&
                    x.BoundaryTime == bestCandidate.BoundaryTime)
                : null;

            int? secondarySegmentId = secondaryCandidate?.WorkPlanSegmentId;
            string? secondarySegmentName = secondaryCandidate?.SegmentName;
            AttendanceClockType secondaryClockType = secondaryCandidate?.ClockType ?? AttendanceClockType.Unresolved;
            string? secondaryPurpose = secondaryCandidate?.SchedulePurpose;
            string? secondaryLogicalType = secondaryCandidate is not null ? $"{secondaryCandidate.SchedulePurpose}{secondaryCandidate.ClockType}" : null;
            string? secondaryMethod = secondaryCandidate is not null ? "SharedCoincidentEnd" : null;
            string? secondaryReason = secondaryCandidate is not null
                ? $"Shared coincident check-out for {secondaryCandidate.SegmentName}."
                : null;

            if (secondarySegmentId == null &&
                bestCandidate.SchedulePurpose == "Duty" &&
                bestCandidate.ClockType == AttendanceClockType.CheckOut &&
                pairedBreakSegment != null)
            {
                secondarySegmentId = pairedBreakSegment.WorkPlanSegmentId;
                secondarySegmentName = pairedBreakSegment.Name;
                secondaryClockType = AttendanceClockType.BreakOut;
                secondaryPurpose = "Official";
                secondaryLogicalType = "OfficialBreakOut";
                secondaryMethod = "SharedDelayedDutyExitAndLunch";
                secondaryReason = $"Official lunch break out shared with {bestCandidate.SegmentName} check-out.";
            }

            return new AttendancePlanResolutionResult
            {
                WorkPlanId = workPlanId,
                WorkPlanSegmentId = bestCandidate.WorkPlanSegmentId,
                JobId = jobId,
                SchedulePurpose = bestCandidate.SchedulePurpose,
                SegmentName = bestCandidate.SegmentName,
                ClockType = bestCandidate.ClockType,
                LogicalEventType = $"{bestCandidate.SchedulePurpose}{bestCandidate.ClockType}",
                MatchMethod = clockTime >= bestCandidate.WindowStart && clockTime <= bestCandidate.WindowEnd
                    ? $"{bestCandidate.SchedulePurpose}BoundaryWindow"
                    : "DelayedBoundaryWindow",
                MatchReason = $"Matched to '{bestCandidate.SegmentName}' ({bestCandidate.SchedulePurpose}) as {bestCandidate.ClockType}.",
                ConfidenceState = secondarySegmentId.HasValue ? "PolicyShared" : "Deterministic",
                State = AttendancePlanResolutionState.Resolved,
                BoundaryTime = bestCandidate.BoundaryTime,
                DistanceMinutes = bestCandidate.DistanceMinutes,
                SecondaryWorkPlanSegmentId = secondarySegmentId,
                SecondarySegmentName = secondarySegmentName,
                SecondaryClockType = secondaryClockType,
                SecondarySchedulePurpose = secondaryPurpose,
                SecondaryLogicalEventType = secondaryLogicalType,
                SecondaryMatchMethod = secondaryMethod,
                SecondaryMatchReason = secondaryReason,
                Message = $"Attendance event resolved to '{bestCandidate.SegmentName}' as {bestCandidate.ClockType}."
            };
        }

        private static BoundaryCandidate SelectBestCandidate(
            List<BoundaryCandidate> candidates,
            DateTime clockTime)
        {
            static IOrderedEnumerable<BoundaryCandidate> Nearest(IEnumerable<BoundaryCandidate> rows) =>
                rows.OrderBy(x => x.DistanceMinutes).ThenBy(x => x.SchedulePurpose == "Official" ? 0 : 1);

            static bool IsCheckOutResolution(BoundaryCandidate x, DateTime clock) =>
                x.ClockType == AttendanceClockType.CheckOut &&
                (
                    (clock > x.WindowEnd && clock <= x.WindowEnd.AddMinutes(LateCheckOutResolutionMinutes))
                    ||
                    (x.HasOpenCheckIn &&
                     clock >= x.BoundaryTime.AddMinutes(-EarlyCheckOutResolutionMinutes) &&
                     clock <= x.BoundaryTime.AddMinutes(LateCheckOutResolutionMinutes))
                );

            static bool IsStaleCheckIn(BoundaryCandidate x) =>
                x.ClockType == AttendanceClockType.CheckIn && x.HasOpenCheckIn;

            var exact = candidates
                .Where(x => clockTime >= x.WindowStart && clockTime <= x.WindowEnd)
                .ToList();

            var exactFresh = exact.Where(x => !IsStaleCheckIn(x)).ToList();
            var exactStale = exact.Where(IsStaleCheckIn).ToList();

            return Nearest(exactFresh.Where(x => x.SchedulePurpose == "Official" && x.IsMandatory)).FirstOrDefault()
                ?? Nearest(exactFresh.Where(x => x.SchedulePurpose == "Official")).FirstOrDefault()
                ?? Nearest(candidates.Where(x => IsCheckOutResolution(x, clockTime))).FirstOrDefault()
                ?? Nearest(exactFresh).FirstOrDefault()
                ?? Nearest(exactStale.Where(x => x.SchedulePurpose == "Official" && x.IsMandatory)).FirstOrDefault()
                ?? Nearest(exactStale.Where(x => x.SchedulePurpose == "Official")).FirstOrDefault()
                ?? Nearest(exactStale).FirstOrDefault()
                ?? candidates
                    .Where(x => x.ClockType == AttendanceClockType.CheckIn &&
                        x.ExtendedWindowEnd.HasValue &&
                        clockTime >= x.WindowStart &&
                        clockTime <= x.ExtendedWindowEnd.Value)
                    .OrderBy(x => x.DistanceMinutes)
                    .ThenBy(x => IsStaleCheckIn(x) ? 1 : 0)
                    .ThenBy(x => x.SchedulePurpose == "Official" ? 0 : 1)
                    .FirstOrDefault()
                ?? Nearest(candidates).First();
        }

        private static string ScheduleGroupKey(string? schedulePurpose, int? templateId) =>
            $"{schedulePurpose ?? string.Empty}|{templateId?.ToString() ?? "0"}";

        private sealed class BoundaryCandidate
        {
            public int WorkPlanSegmentId { get; set; }
            public string SegmentName { get; set; } = string.Empty;
            public string SchedulePurpose { get; set; } = string.Empty;
            public bool IsMandatory { get; set; }
            public string GroupKey { get; set; } = string.Empty;
            public bool HasOpenCheckIn { get; set; }
            public AttendanceClockType ClockType { get; set; }
            public DateTime BoundaryTime { get; set; }
            public DateTime WindowStart { get; set; }
            public DateTime WindowEnd { get; set; }
            public DateTime? ExtendedWindowEnd { get; set; }
            public double DistanceMinutes { get; set; }
        }

        private static bool IsPairedBreakSegment(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            return code.Trim().ToUpperInvariant() switch
            {
                "LUNCH" => true,
                _ => false
            };
        }

        private static bool RequiresCheckIn(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            return code.Trim().ToUpperInvariant() switch
            {
                "CHECK_IN" => true,
                "DUTY_CHECK_IN" => true,
                "BREAK_END" => true,
                _ => false
            };
        }

        private static bool RequiresCheckOut(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            return code.Trim().ToUpperInvariant() switch
            {
                "CHECK_OUT" => true,
                "DUTY_CHECK_OUT" => true,
                "BREAK_START" => true,
                _ => false
            };
        }
    }
}
