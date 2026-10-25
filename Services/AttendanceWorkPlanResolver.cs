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

            // GenerateOrGetAsync does both:
            //
            // 1. Return an existing WorkPlan if one exists.
            // 2. Otherwise generate one from JobWorkTemplate.
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

                    State =
                        AttendancePlanResolutionState.NoWorkPlan,

                    ClockType =
                        AttendanceClockType.Unresolved,

                    Message =
                        "No applicable work plan or work-template assignment " +
                        "was found for this attendance event."
                };
            }

            var workPlanId =
                selectedPlanId.Value;
 

            var amendmentRows = await HRM.Helpers.OfficialScheduleAmendments.LoadAsync(db, organisationBusinessEntityId,
                clockTime.Date.AddDays(-1), clockTime.Date, cancellationToken);
            var segmentEntities = await db.WorkPlanSegments.AsNoTracking().Include(x => x.WorkSegmentType)
                .Where(x => x.WorkPlanId == workPlanId && x.IsValid).OrderBy(x => x.SequenceNumber)
                .Select(x => new { x.WorkPlanSegmentId, x.Name, x.StartDateTime, x.EndDateTime, x.SequenceNumber,
                    x.SchedulePurpose, TemplateId = x.WorkTemplateSegment == null ? (int?)null : x.WorkTemplateSegment.WorkTemplateId,
                    WorkSegmentTypeCode = x.WorkSegmentType.Code, x.RequiresAttendance, x.GraceBeforeMinutes, x.GraceAfterMinutes })
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
                return new { x.WorkPlanSegmentId, x.Name, StartDateTime = timing.Start, EndDateTime = timing.End,
                    x.SequenceNumber, x.TemplateId, x.SchedulePurpose, x.WorkSegmentTypeCode, x.RequiresAttendance,
                    GraceBeforeMinutes = timing.GraceBefore, x.GraceAfterMinutes };
            }).ToList();

            if (segments.Count == 0)
            {
                return new AttendancePlanResolutionResult
                {
                    WorkPlanId = workPlanId,
                    JobId = jobId,

                    State =
                        AttendancePlanResolutionState.NoSegment,

                    ClockType =
                        AttendanceClockType.Unresolved,

                    Message =
                         "The work plan does not contain any valid segments."
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
            // LUNCH BREAK
            //
            // Lunch Break is state-based:
            //
            // Current state = CheckIn  -> Lunch event = CheckOut
            // Current state = CheckOut -> Lunch event = CheckIn
            // ============================================================

            // Explicit shift/duty clock boundaries take precedence over a simultaneous break.
            var hasExplicitBoundary = segments.Any(x => x.RequiresAttendance &&
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


                // No previous IN/OUT state.
                // We cannot safely determine the direction.
                if (!lastAttendanceState.HasValue)
                {
                    return new AttendancePlanResolutionResult
                    {
                        WorkPlanId =
                            workPlanId,

                        WorkPlanSegmentId =
                            pairedBreakSegment.WorkPlanSegmentId,

                        JobId =
                            jobId,

                        SegmentName =
                            pairedBreakSegment.Name,

                        ClockType =
                            AttendanceClockType.Unresolved,

                        State =
                            AttendancePlanResolutionState.Unresolved,

                        BoundaryTime =
                            clockTime,

                        DistanceMinutes =
                            0,

                        Message =
                            $"Cannot determine attendance direction for " +
                            $"'{pairedBreakSegment.Name}' because no previous " +
                            $"CheckIn or CheckOut exists."
                    };
                }


                AttendanceClockType lunchClockType;

                if (lastAttendanceState.Value ==
                    AttendanceClockType.CheckIn)
                {
                    // Employee is currently IN.
                    // First Lunch Break clock means OUT.
                    lunchClockType =
                        AttendanceClockType.CheckOut;
                }
                else
                {
                    // Employee is currently OUT.
                    // Next Lunch Break clock means IN.
                    lunchClockType =
                        AttendanceClockType.CheckIn;
                }


                return new AttendancePlanResolutionResult
                {
                    WorkPlanId =
                        workPlanId,

                    WorkPlanSegmentId =
                        pairedBreakSegment.WorkPlanSegmentId,

                    JobId =
                        jobId,

                    SegmentName =
                        pairedBreakSegment.Name,

                    ClockType =
                        lunchClockType,

                    State =
                        AttendancePlanResolutionState.Resolved,

                    BoundaryTime =
                        clockTime,

                    DistanceMinutes =
                        0,

                    Message =
                        lunchClockType ==
                            AttendanceClockType.CheckOut

                            ? $"Checked out for '{pairedBreakSegment.Name}'."

                            : $"Checked in from '{pairedBreakSegment.Name}'."
                };
            }


            // ============================================================
            // NORMAL CHECK-IN / CHECK-OUT BOUNDARIES
            // ============================================================

 

            var candidates =
                new List<BoundaryCandidate>();


            foreach (var segment in segments.Where(x => x.RequiresAttendance))
            {
                var code =
                    segment.WorkSegmentTypeCode;


                // ========================================================
                // CHECK-IN BOUNDARY
                //
                // StartDateTime = expected check-in
                // EndDateTime   = end of permitted check-in window
                // ========================================================

                if (RequiresCheckIn(code))
                {
                    var expectedTime =
                        segment.StartDateTime;

                    // Resolution window is independent of attendance grace.
                    var windowStart =
                        expectedTime.AddMinutes(
                            -EarlyCheckInResolutionMinutes);

                    var windowEnd = segment.EndDateTime;
                    if (segment.SchedulePurpose is "Duty" or "Official")
                    {
                        var dutyEnd = segments.Where(s => s.SchedulePurpose == segment.SchedulePurpose && s.TemplateId == segment.TemplateId &&
                            RequiresCheckOut(s.WorkSegmentTypeCode) && s.StartDateTime > expectedTime)
                            .Select(s => (DateTime?)s.StartDateTime).Min();
                        if (dutyEnd.HasValue && dutyEnd.Value.AddTicks(-1) > windowEnd) windowEnd = dutyEnd.Value.AddTicks(-1);
                    }

                    if (clockTime >= windowStart &&
                        clockTime <= windowEnd)
                    {
                        candidates.Add(
                            new BoundaryCandidate
                            {
                                WorkPlanSegmentId =
                                    segment.WorkPlanSegmentId,

                                SegmentName =
                                    segment.Name,

                                ClockType =
                                    AttendanceClockType.CheckIn,

                                BoundaryTime =
                                    expectedTime,

                                WindowStart =
                                    windowStart,

                                WindowEnd =
                                    windowEnd,

                                DistanceMinutes =
                                    Math.Abs(
                                        (clockTime - expectedTime)
                                        .TotalMinutes)
                            });
                    }
                }

                // ========================================================
                // CHECK-OUT BOUNDARY
                //
                // StartDateTime = expected checkout
                // EndDateTime   = latest permitted checkout
                // ========================================================

                if (RequiresCheckOut(code))
                {
                    var expectedTime =
                        segment.StartDateTime;

                    var windowStart =
                        segment.StartDateTime
                            .AddMinutes(
                                -segment.GraceBeforeMinutes);

                    var windowEnd =
                        segment.EndDateTime;


                    if (clockTime >= windowStart &&
                        clockTime <= windowEnd)
                    {
                        candidates.Add(
                            new BoundaryCandidate
                            {
                                WorkPlanSegmentId =
                                    segment.WorkPlanSegmentId,

                                SegmentName =
                                    segment.Name,

                                ClockType =
                                    AttendanceClockType.CheckOut,

                                BoundaryTime =
                                    expectedTime,

                                WindowStart =
                                    windowStart,

                                WindowEnd =
                                    windowEnd,

                                DistanceMinutes =
                                    Math.Abs(
                                        (clockTime - expectedTime)
                                        .TotalMinutes)
                            });
                    }
                }
            }


            // ============================================================
            // NO MATCHING BOUNDARY
            // ============================================================

            if (candidates.Count == 0)
            {
                return new AttendancePlanResolutionResult
                {
                    WorkPlanId =
                        workPlanId,

                    JobId =
                        jobId,

                    State =
                        AttendancePlanResolutionState
                            .OutsideResolutionWindow,

                    ClockType =
                        AttendanceClockType.Unresolved,

                    BoundaryTime =
                        clockTime,

                    DistanceMinutes =
                        0,

                    Message =
                        $"Attendance event at {clockTime:HH:mm} " +
                        $"does not fall inside an applicable " +
                        $"attendance window."
                };
            }


            // ============================================================
            // CHOOSE BEST MATCH
            //
            // If windows overlap, choose the boundary whose expected
            // time is closest to the physical clock.
            // ============================================================

            var bestCandidate =
                candidates
                    .OrderBy(x =>
                        x.DistanceMinutes)
                    .First();


            // Match each punch to the nearest scheduled boundary. A prior official
            // check-in never changes the direction of a separate duty check-in.



            return new AttendancePlanResolutionResult
            {
                WorkPlanId =
                    workPlanId,

                WorkPlanSegmentId =
                    bestCandidate.WorkPlanSegmentId,

                JobId =
                    jobId,

                SegmentName =
                    bestCandidate.SegmentName,

                ClockType =
                    bestCandidate.ClockType,

                State =
                    AttendancePlanResolutionState.Resolved,

                BoundaryTime =
                    bestCandidate.BoundaryTime,

                DistanceMinutes =
                    bestCandidate.DistanceMinutes,

                Message =
                    $"Attendance event resolved to " +
                    $"'{bestCandidate.SegmentName}' as " +
                    $"{bestCandidate.ClockType}."
            };
        }

    

        private sealed class BoundaryCandidate
        {
            public int WorkPlanSegmentId { get; set; }

            public string SegmentName { get; set; }
                = string.Empty;

            public AttendanceClockType ClockType { get; set; }

            public DateTime BoundaryTime { get; set; }

            public DateTime WindowStart { get; set; }

            public DateTime WindowEnd { get; set; }

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



        //private static bool IsPairedBreakSegment(string? code)
        //{
        //    return string.Equals(
        //        code,
        //        "LUNCH",
        //        StringComparison.OrdinalIgnoreCase);
        //}


        private static bool RequiresCheckIn(
            string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            return code
                .Trim()
                .ToUpperInvariant() switch
            {
                "CHECK_IN" => true,
                "DUTY_CHECK_IN" => true,
                "BREAK_END" => true,

                _ => false
            };
        }


        private static bool RequiresCheckOut(
            string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            return code
                .Trim()
                .ToUpperInvariant() switch
            {
                "CHECK_OUT" => true,
                "DUTY_CHECK_OUT" => true,
                "BREAK_START" => true,

                _ => false
            };
        }



    }
}
