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

            if (generatedPlan == null)
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

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var workPlanId =
                generatedPlan.WorkPlanId;
 

            var segments =
                await db.WorkPlanSegments
                    .AsNoTracking()
                    .Where(x =>
                        x.WorkPlanId == workPlanId &&
                        x.IsValid)
                    .OrderBy(x => x.SequenceNumber)
                    .Select(x => new
                    {
                        x.WorkPlanSegmentId,
                        x.Name,
                        x.StartDateTime,
                        x.EndDateTime,
                        x.SequenceNumber,
                            WorkSegmentTypeCode = x.WorkSegmentType.Code,
                        x.RequiresAttendance,
                        x.GraceBeforeMinutes,
                        x.GraceAfterMinutes
                    })
                    .ToListAsync(cancellationToken);

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
                        IsPairedBreakSegment(x.WorkSegmentTypeCode))
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

            if (pairedBreakSegment != null)
            {
                var lastAttendanceState =
                    await db.AttendanceLogResolutions
                        .AsNoTracking()
                        .Where(x =>
                            x.WorkPlanId == workPlanId &&
                            x.IsValid &&
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


            foreach (var segment in segments)
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
