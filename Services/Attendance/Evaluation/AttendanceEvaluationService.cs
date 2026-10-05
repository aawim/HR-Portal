using HRM.Components.Admin.Settings.Workplan;
using HRM.Data;
using HRM.DTOs.Attendance;
using HRM.Enum;
using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.Services.Attendance.Evaluation;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Attendance
{
    public sealed class AttendanceEvaluationService
        : IAttendanceEvaluationService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;

        public AttendanceEvaluationService(
            IDbContextFactory<HrmTeContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<AttendanceDailyEvaluationDto> EvaluateAsync(
                  int individualId,
                  int jobId,
                  DateTime workDate,
                  CancellationToken cancellationToken = default)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var date =
                workDate.Date;

            var evaluationTime =
                DateTime.Now;





            // ========================================================
            // LOAD WORK PLAN
            // ========================================================

            var workPlan =
                await db.WorkPlans
                    .AsNoTracking()
                    .Include(x => x.WorkPlanSegments)
                        .ThenInclude(x => x.WorkSegmentType)
                    .Include(x => x.AttendanceLogResolutions)
                        .ThenInclude(x => x.AttendanceLog)
                    .Where(x =>
                        x.IndividualId == individualId &&
                        x.JobId == jobId &&
                        x.WorkDate.Date == date &&
                        x.IsValid)
                    .OrderByDescending(x => x.IsManual)
                    .ThenByDescending(x => x.Version)
                    .FirstOrDefaultAsync(
                        cancellationToken);


            // ========================================================
            // NO WORK PLAN
            // ========================================================

            if (workPlan == null)
            {
                return new AttendanceDailyEvaluationDto
                {
                    IndividualId =
                        individualId,

                    JobId =
                        jobId,

                    WorkDate =
                        date,

                    HasWorkPlan =
                        false,

                    RequiresAttendance =
                        false,

                    Status =
                        date > evaluationTime.Date
                            ? AttendanceDailyStatus.Future
                            : AttendanceDailyStatus.Unknown,

                    IsComplete =
                        false,

                    HasException =
                        date <= evaluationTime.Date,

                    Issues =
                    [
                        new AttendanceEvaluationIssueDto
                        {
                            Type =
                                AttendanceEvaluationIssueType
                                    .NoWorkPlan,

                            Severity =
                                AttendanceEvaluationIssueSeverity
                                    .Warning,

                            Message =
                                "No work plan was found."
                        }
                    ]
                };
            }


            // ========================================================
            // SEGMENTS
            // ========================================================

            var segments =
                workPlan.WorkPlanSegments
                    .Where(x => x.IsValid)
                    .OrderBy(x => x.SequenceNumber)
                    .ThenBy(x => x.StartDateTime)
                    .ToList();
            var attendanceSegments = segments.ToList();
            var requiresAttendance = attendanceSegments.Any();
 

            // ========================================================
            // RESOLVED ATTENDANCE EVENTS
            // ========================================================

            var resolvedEvents =
                workPlan.AttendanceLogResolutions
                    .Where(x =>
                        x.IsValid &&
                        x.AttendanceLog != null &&
                        x.AttendanceClockTypeId.HasValue &&
                        (
                            x.AttendanceClockTypeId.Value ==
                                (int)AttendanceClockType.CheckIn
                            ||
                            x.AttendanceClockTypeId.Value ==
                                (int)AttendanceClockType.CheckOut
                        ))
                    .Select(x =>
                        new ResolvedAttendanceEvent
                        {
                            AttendanceLogId =
                                x.AttendanceLogId,

                            WorkPlanSegmentId =
                                x.WorkPlanSegmentId,

                            ClockType =
                                (AttendanceClockType)
                                x.AttendanceClockTypeId!.Value,

                            LogDateTime =
                                x.AttendanceLog.Date
                        })
                    .OrderBy(x => x.LogDateTime)
                    .ToList();

 


        //    var lunchCheckOut =
        //        resolvedEvents
        //      .Where(x =>
        //    x.ClockType == AttendanceClockType.CheckOut)
        //.OrderBy(x => x.AttendanceLogId)
        //.FirstOrDefault();



        //    var lunchCheckIn =
        //        resolvedEvents
        //      .Where(x =>
        //    x.ClockType == AttendanceClockType.CheckIn)
        //.OrderBy(x => x.AttendanceLogId)
        //.FirstOrDefault();

 









            // ========================================================
            // EFFECTIVE CHECK-IN
            //
            // EARLIEST valid CheckIn wins.
            // ========================================================

            var effectiveCheckIn =
                resolvedEvents
                    .Where(x =>
                        x.ClockType ==
                        AttendanceClockType.CheckIn)
                    .OrderBy(x => x.LogDateTime)
                    .FirstOrDefault();


            // ========================================================
            // EFFECTIVE CHECK-OUT
            //
            // LATEST valid CheckOut wins.
            // ========================================================

            var effectiveCheckOut =
                resolvedEvents
                    .Where(x =>
                        x.ClockType ==
                        AttendanceClockType.CheckOut)
                    .OrderByDescending(x => x.LogDateTime)
                    .FirstOrDefault();


            // ========================================================
            // RAW CLOCK COUNTS
            // ========================================================

            var allResolutions =
                workPlan.AttendanceLogResolutions
                    .Where(x =>
                        x.IsValid &&
                        x.AttendanceLog != null)
                    .ToList();


            var rawClockCount =
                allResolutions
                    .Select(x => x.AttendanceLogId)
                    .Distinct()
                    .Count();


            var resolvedClockCount =
                allResolutions
                    .Count(x =>
                        x.AttendanceClockTypeId.HasValue &&
                        x.AttendanceClockTypeId.Value !=
                            (int)AttendanceClockType.Unresolved);


            var unresolvedClockCount =
                allResolutions
                    .Count(x =>
                        !x.AttendanceClockTypeId.HasValue ||
                        x.AttendanceClockTypeId.Value ==
                            (int)AttendanceClockType.Unresolved);


            // ========================================================
            // PLANNED START
            // ========================================================

            var checkInBoundary =
                attendanceSegments
                    .Where(x =>
                        RequiresCheckIn(
                            x.WorkSegmentType?.Code))
                    .OrderBy(x => x.StartDateTime)
                    .FirstOrDefault();


            // ========================================================
            // PLANNED END
            //
            // IMPORTANT:
            //
            // CHECK_OUT.StartDateTime = expected checkout
            // CHECK_OUT.EndDateTime   = latest allowed checkout
            // ========================================================

            var checkOutBoundary =
                attendanceSegments
                    .Where(x =>
                        RequiresCheckOut(
                            x.WorkSegmentType?.Code))
                    .OrderByDescending(
                        x => x.StartDateTime)
                    .FirstOrDefault();


            DateTime? plannedStart =
                checkInBoundary?.StartDateTime;


            DateTime? plannedEnd =
                checkOutBoundary?.StartDateTime;


            // ========================================================
            // PLANNED MINUTES
            // ========================================================

            var plannedMinutes = 0;

            if (plannedStart.HasValue &&
                plannedEnd.HasValue &&
                plannedEnd.Value > plannedStart.Value)
            {
                plannedMinutes =
                    CalculateMinutes(
                        plannedStart.Value,
                        plannedEnd.Value);
            }


            // ========================================================
            // WORKED MINUTES
            //
            // For now:
            //
            // First CheckIn -> Last CheckOut
            //
            // Break deduction can be introduced later.
            // ========================================================

            var workedMinutes = CalculateWorkedMinutes(resolvedEvents);


            // ========================================================
            // EVALUATE INDIVIDUAL SEGMENTS
            // ========================================================

            var evaluatedSegments =
                new List<AttendanceEvaluatedSegmentDto>();


            foreach (var segment in segments)
            {
                var evaluatedSegment =
                    EvaluateSegment(
                        segment,
                        resolvedEvents,
                        evaluationTime);

                evaluatedSegments.Add(
                    evaluatedSegment);
            }


            // ========================================================
            // COLLECT ISSUES
            // ========================================================

            var issues =
                evaluatedSegments
                    .SelectMany(x => x.Issues)
                    .ToList();


            // ========================================================
            // DAILY LATE MINUTES
            // ========================================================

            var lateMinutes =
                evaluatedSegments
                    .Sum(x => x.LateMinutes);


            // ========================================================
            // DAILY EARLY DEPARTURE
            // ========================================================

            var earlyDepartureMinutes =
                evaluatedSegments
                    .Sum(x =>
                        x.EarlyDepartureMinutes);


            // ========================================================
            // COMPLETE
            //
            // Only attendance boundary segments determine whether
            // physical attendance is complete.
            // ========================================================

            var requiredBoundarySegments =
                attendanceSegments
                    .Where(x =>
                        IsClockBoundary(
                            x.WorkSegmentType?.Code))
                    .ToList();


            var isComplete =
                !requiresAttendance ||
                (
                    requiredBoundarySegments.Any() &&
                    requiredBoundarySegments.All(
                        segment =>
                            evaluatedSegments
                                .First(x =>
                                    x.WorkPlanSegmentId ==
                                    segment.WorkPlanSegmentId)
                                .IsComplete)
                );


            // ========================================================
            // DAILY STATUS
            // ========================================================

            var status =
                DetermineDailyStatus(
                    date,
                    evaluationTime,
                    attendanceSegments,
                    evaluatedSegments,
                    resolvedEvents,
                    requiresAttendance,
                    isComplete);


            // ========================================================
            // EXCEPTION
            // ========================================================

            var hasException =
                issues.Any(x =>
                    x.Severity ==
                        AttendanceEvaluationIssueSeverity.Warning
                    ||
                    x.Severity ==
                        AttendanceEvaluationIssueSeverity.Error);


            // ========================================================
            // RESULT
            // ========================================================

            return new AttendanceDailyEvaluationDto
            {
                IndividualId =
                    individualId,

                JobId =
                    jobId,

                OrganisationId =
                    workPlan.OrganisationBusinessEntityId,

                WorkDate =
                    date,

                WorkPlanId =
                    workPlan.WorkPlanId,

                HasWorkPlan =
                    true,

                RequiresAttendance =
                    requiresAttendance,

                RequiresCheckOut =
                    checkOutBoundary != null,

                PlannedStart =
                    plannedStart,

                PlannedEnd =
                    plannedEnd,

                PlannedMinutes =
                    plannedMinutes,

                FirstCheckIn =
                    effectiveCheckIn?.LogDateTime,

                LastCheckOut =
                    effectiveCheckOut?.LogDateTime,

                WorkedMinutes =
                    workedMinutes,

                LateMinutes =
                    lateMinutes,

                EarlyDepartureMinutes =
                    earlyDepartureMinutes,

                OvertimeMinutes =
                    0,

                RawClockCount =
                    rawClockCount,

                ResolvedClockCount =
                    resolvedClockCount,

                UnresolvedClockCount =
                    unresolvedClockCount,

                Status =
                    status,

                IsComplete =
                    isComplete,

                HasException =
                    hasException,

                Issues =
                    issues,

                Segments =
                    evaluatedSegments
            };
        }


        // ============================================================
        // EVALUATE SEGMENT
        // ============================================================

        private static AttendanceEvaluatedSegmentDto EvaluateSegment(
            WorkPlanSegment segment,
            IReadOnlyCollection<ResolvedAttendanceEvent> events,
            DateTime evaluationTime)
        {
            var result =
                new AttendanceEvaluatedSegmentDto
                {
                    WorkPlanSegmentId =
                        segment.WorkPlanSegmentId,

                    WorkPlanId =
                        segment.WorkPlanId,

                    WorkSegmentTypeId =
                        segment.WorkSegmentTypeId,

                    Name =
                        segment.Name,

                    SequenceNumber =
                        segment.SequenceNumber,

                    IsMandatory =
                        segment.IsMandatory,

                    RequiresAttendance =
                        segment.RequiresAttendance,

                    IsPaid =
                        segment.IsPaid,

                    StartDateTime =
                        segment.StartDateTime,

                    EndDateTime =
                        segment.EndDateTime,

                    GraceBeforeMinutes =
                        segment.GraceBeforeMinutes,

                    GraceAfterMinutes =
                        segment.GraceAfterMinutes,

                    PlannedMinutes =
                        CalculateMinutes(
                            segment.StartDateTime,
                            segment.EndDateTime)
                };


            // ========================================================
            // SEGMENT DOES NOT REQUIRE ATTENDANCE
            // ========================================================

            if (!segment.RequiresAttendance)
            {
                result.IsComplete = true;
                return result;
            }


            var code =
                segment.WorkSegmentType?.Code;


            var requiresCheckIn =
                RequiresCheckIn(code);


            var requiresCheckOut =
                RequiresCheckOut(code);


            // ========================================================
            // WORK / ACTIVITY SEGMENT
            //
            // WORK, LUNCH, MEETING, etc. are not themselves
            // physical clock boundaries.
            // ========================================================

            if (!requiresCheckIn &&
                !requiresCheckOut)
            {
                result.IsComplete = true;
                return result;
            }


            // ========================================================
            // EVENTS RESOLVED TO THIS SEGMENT
            // ========================================================

            var segmentEvents =
                events
                    .Where(x =>
                        x.WorkPlanSegmentId ==
                        segment.WorkPlanSegmentId)
                    .OrderBy(x =>
                        x.LogDateTime)
                    .ToList();


            // ========================================================
            // CHECK IN
            //
            // EARLIEST CheckIn for the boundary.
            // ========================================================

            ResolvedAttendanceEvent? checkInEvent =
                segmentEvents
                    .Where(x =>
                        x.ClockType ==
                        AttendanceClockType.CheckIn)
                    .OrderBy(x =>
                        x.LogDateTime)
                    .FirstOrDefault();


            // ========================================================
            // CHECK OUT
            //
            // LATEST CheckOut for the boundary.
            // ========================================================

            ResolvedAttendanceEvent? checkOutEvent =
                segmentEvents
                    .Where(x =>
                        x.ClockType ==
                        AttendanceClockType.CheckOut)
                    .OrderByDescending(x =>
                        x.LogDateTime)
                    .FirstOrDefault();


            var checkIn =
                checkInEvent?.LogDateTime;


            var checkOut =
                checkOutEvent?.LogDateTime;


            result.CheckIn =
                checkIn;


            result.CheckOut =
                checkOut;


            // ========================================================
            // CHECK-IN EVALUATION
            // ========================================================

            if (requiresCheckIn)
            {
                var expectedCheckIn =
                    segment.StartDateTime;


                // ----------------------------------------------------
                // No CheckIn
                // ----------------------------------------------------

                if (!checkIn.HasValue)
                {
                    // The configured segment end represents the
                    // permitted resolution window.

                    var latestAllowedCheckIn =
                        segment.EndDateTime;


                    if (evaluationTime >
                        latestAllowedCheckIn)
                    {
                        result.Issues.Add(
                            new AttendanceEvaluationIssueDto
                            {
                                Type =
                                    AttendanceEvaluationIssueType
                                        .MissingCheckIn,

                                Severity =
                                    AttendanceEvaluationIssueSeverity
                                        .Error,

                                Message =
                                    $"Check in is missing for " +
                                    $"'{segment.Name}'.",

                                WorkPlanSegmentId =
                                    segment.WorkPlanSegmentId,

                                ExpectedTime =
                                    expectedCheckIn
                            });
                    }
                }

                // ----------------------------------------------------
                // CheckIn exists
                // ----------------------------------------------------

                else
                {
                    var latestOnTime =
                        expectedCheckIn
                            .AddMinutes(
                                segment.GraceAfterMinutes);


                    if (checkIn.Value >
                        latestOnTime)
                    {
                        result.LateMinutes =
                            CalculateMinutes(
                                latestOnTime,
                                checkIn.Value);


                        result.Issues.Add(
                            new AttendanceEvaluationIssueDto
                            {
                                Type =
                                    AttendanceEvaluationIssueType
                                        .LateCheckIn,

                                Severity =
                                    AttendanceEvaluationIssueSeverity
                                        .Warning,

                                Message =
                                    $"Late check in for " +
                                    $"'{segment.Name}' by " +
                                    $"{result.LateMinutes} minute(s).",

                                WorkPlanSegmentId =
                                    segment.WorkPlanSegmentId,

                                AttendanceLogId =
                                    checkInEvent?
                                        .AttendanceLogId,

                                ExpectedTime =
                                    expectedCheckIn,

                                ActualTime =
                                    checkIn.Value,

                                DifferenceMinutes =
                                    result.LateMinutes
                            });
                    }
                }
            }


            // ========================================================
            // CHECK-OUT EVALUATION
            //
            // IMPORTANT:
            //
            // StartDateTime = expected checkout
            // EndDateTime   = latest permitted checkout
            // ========================================================

            if (requiresCheckOut)
            {
                var expectedCheckOut =
                    segment.StartDateTime;


                var latestAllowedCheckOut =
                    segment.EndDateTime;


                // ----------------------------------------------------
                // No CheckOut
                // ----------------------------------------------------

                if (!checkOut.HasValue)
                {
                    // Do not mark missing while the permitted
                    // checkout window is still open.

                    if (evaluationTime >
                        latestAllowedCheckOut)
                    {
                        result.Issues.Add(
                            new AttendanceEvaluationIssueDto
                            {
                                Type =
                                    AttendanceEvaluationIssueType
                                        .MissingCheckOut,

                                Severity =
                                    AttendanceEvaluationIssueSeverity
                                        .Error,

                                Message =
                                    $"Check out is missing for " +
                                    $"'{segment.Name}'.",

                                WorkPlanSegmentId =
                                    segment.WorkPlanSegmentId,

                                ExpectedTime =
                                    expectedCheckOut
                            });
                    }
                }

                // ----------------------------------------------------
                // CheckOut exists
                // ----------------------------------------------------

                else
                {
                    var earliestAllowed =
                        expectedCheckOut
                            .AddMinutes(
                                -segment.GraceBeforeMinutes);


                    if (checkOut.Value <
                        earliestAllowed)
                    {
                        result.EarlyDepartureMinutes =
                            CalculateMinutes(
                                checkOut.Value,
                                earliestAllowed);


                        result.Issues.Add(
                            new AttendanceEvaluationIssueDto
                            {
                                Type =
                                    AttendanceEvaluationIssueType
                                        .EarlyCheckOut,

                                Severity =
                                    AttendanceEvaluationIssueSeverity
                                        .Warning,

                                Message =
                                    $"Early check out for " +
                                    $"'{segment.Name}' by " +
                                    $"{result.EarlyDepartureMinutes} minute(s).",

                                WorkPlanSegmentId =
                                    segment.WorkPlanSegmentId,

                                AttendanceLogId =
                                    checkOutEvent?
                                        .AttendanceLogId,

                                ExpectedTime =
                                    expectedCheckOut,

                                ActualTime =
                                    checkOut.Value,

                                DifferenceMinutes =
                                    result.EarlyDepartureMinutes
                            });
                    }
                }
            }


            // ========================================================
            // WORKED MINUTES FOR SEGMENT
            //
            // Usually 0 for standalone boundary segments.
            // ========================================================

            if (checkIn.HasValue &&
                checkOut.HasValue &&
                checkOut.Value > checkIn.Value)
            {
                result.WorkedMinutes =
                    CalculateMinutes(
                        checkIn.Value,
                        checkOut.Value);
            }


            // ========================================================
            // COMPLETE
            // ========================================================

            if (requiresCheckIn &&
                requiresCheckOut)
            {
                result.IsComplete =
                    checkIn.HasValue &&
                    checkOut.HasValue;
            }
            else if (requiresCheckIn)
            {
                result.IsComplete =
                    checkIn.HasValue;
            }
            else if (requiresCheckOut)
            {
                // While the checkout window is open,
                // this boundary is not yet an exception.

                result.IsComplete =
                    checkOut.HasValue ||
                    evaluationTime <=
                        segment.EndDateTime;
            }
            else
            {
                result.IsComplete =
                    true;
            }


            result.HasException =
                result.Issues.Any(x =>
                    x.Severity ==
                        AttendanceEvaluationIssueSeverity.Warning
                    ||
                    x.Severity ==
                        AttendanceEvaluationIssueSeverity.Error);


            return result;
        }


        // ============================================================
        // DETERMINE DAILY STATUS
        // ============================================================

        private static AttendanceDailyStatus DetermineDailyStatus(
            DateTime workDate,
            DateTime evaluationTime,
            IReadOnlyCollection<WorkPlanSegment> attendanceSegments,
            IReadOnlyCollection<AttendanceEvaluatedSegmentDto> evaluatedSegments,
            IReadOnlyCollection<ResolvedAttendanceEvent> events,
            bool requiresAttendance,
            bool isComplete)
        {
            // ========================================================
            // FUTURE DATE
            // ========================================================

            if (workDate.Date >
                evaluationTime.Date)
            {
                return AttendanceDailyStatus.Future;
            }


            // ========================================================
            // NO ATTENDANCE REQUIRED
            // ========================================================

            if (!requiresAttendance)
            {
                return AttendanceDailyStatus.RestDay;
            }


            // ========================================================
            // BOUNDARIES
            // ========================================================

            var checkInBoundary =
                attendanceSegments
                    .Where(x =>
                        RequiresCheckIn(
                            x.WorkSegmentType?.Code))
                    .OrderBy(x =>
                        x.StartDateTime)
                    .FirstOrDefault();


            var checkOutBoundary =
                attendanceSegments
                    .Where(x =>
                        RequiresCheckOut(
                            x.WorkSegmentType?.Code))
                    .OrderByDescending(x =>
                        x.StartDateTime)
                    .FirstOrDefault();


            // ========================================================
            // EVENT FLAGS
            // ========================================================

            var hasCheckIn =
                events.Any(x =>
                    x.ClockType ==
                    AttendanceClockType.CheckIn);


            var hasCheckOut =
                events.Any(x =>
                    x.ClockType ==
                    AttendanceClockType.CheckOut);


            // ========================================================
            // TODAY
            // ========================================================

            if (workDate.Date ==
                evaluationTime.Date)
            {
                // ----------------------------------------------------
                // BEFORE EXPECTED CHECK-IN
                // ----------------------------------------------------

                if (checkInBoundary != null &&
                    evaluationTime <
                    checkInBoundary.StartDateTime)
                {
                    return AttendanceDailyStatus.Future;
                }


                // ----------------------------------------------------
                // EXPECTED DUTY END
                //
                // CHECK_OUT.StartDateTime
                // ----------------------------------------------------

                var expectedDutyEnd =
                    checkOutBoundary?
                        .StartDateTime;


                // ----------------------------------------------------
                // LATEST CHECKOUT
                //
                // CHECK_OUT.EndDateTime
                // ----------------------------------------------------

                var latestCheckOutTime =
                    checkOutBoundary?
                        .EndDateTime;


                // ----------------------------------------------------
                // WITHIN PLANNED DUTY
                // ----------------------------------------------------

                if (expectedDutyEnd.HasValue &&
                    evaluationTime <=
                    expectedDutyEnd.Value)
                {
                    return AttendanceDailyStatus
                        .InProgress;
                }


                // ----------------------------------------------------
                // BOTH BOUNDARIES COMPLETE
                // ----------------------------------------------------

                if (hasCheckIn &&
                    hasCheckOut)
                {
                    return isComplete
                        ? AttendanceDailyStatus.Present
                        : AttendanceDailyStatus.Incomplete;
                }


                // ----------------------------------------------------
                // DUTY ENDED, CHECKOUT WINDOW STILL OPEN
                // ----------------------------------------------------

                if (latestCheckOutTime.HasValue &&
                    evaluationTime <=
                    latestCheckOutTime.Value)
                {
                    if (hasCheckIn)
                    {
                        return AttendanceDailyStatus
                            .InProgress;
                    }


                    return AttendanceDailyStatus
                        .Absent;
                }


                // ----------------------------------------------------
                // CHECKOUT WINDOW CLOSED
                // ----------------------------------------------------

                if (!hasCheckIn)
                {
                    return AttendanceDailyStatus
                        .Absent;
                }


                if (!hasCheckOut)
                {
                    return AttendanceDailyStatus
                        .Incomplete;
                }


                return isComplete
                    ? AttendanceDailyStatus.Present
                    : AttendanceDailyStatus.Incomplete;
            }


            // ========================================================
            // HISTORICAL DATE
            // ========================================================

            if (!hasCheckIn)
            {
                return AttendanceDailyStatus.Absent;
            }


            if (!hasCheckOut)
            {
                return AttendanceDailyStatus.Incomplete;
            }


            return isComplete
                ? AttendanceDailyStatus.Present
                : AttendanceDailyStatus.Incomplete;
        }


        // ============================================================
        // CLOCK BOUNDARY HELPERS
        // ============================================================

        private static bool RequiresCheckIn(
            string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }


            return code
                .Trim()
                .ToUpperInvariant() switch
            {
                "CHECK_IN" =>
                    true,

                "DUTY_CHECK_IN" =>
                    true,

                "BREAK_END" =>
                    true,

                _ =>
                    false
            };
        }


        private static bool RequiresCheckOut(
            string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }


            return code
                .Trim()
                .ToUpperInvariant() switch
            {
                "CHECK_OUT" =>
                    true,

                "DUTY_CHECK_OUT" =>
                    true,

                "BREAK_START" =>
                    true,

                _ =>
                    false
            };
        }


        private static bool IsClockBoundary(
            string? code)
        {
            return
                RequiresCheckIn(code) ||
                RequiresCheckOut(code);
        }


        // ============================================================
        // MINUTE CALCULATION
        // ============================================================

        private static int CalculateMinutes(
            DateTime start,
            DateTime end)
        {
            if (end <= start)
            {
                return 0;
            }


            return Math.Max(
                0,
                (int)Math.Round(
                    (end - start)
                    .TotalMinutes));
        }


        // ============================================================
        // INTERNAL RESOLVED EVENT
        // ============================================================

        private sealed class ResolvedAttendanceEvent
        {
            public int AttendanceLogId { get; set; }

            public int? WorkPlanSegmentId { get; set; }

            public AttendanceClockType ClockType { get; set; }

            public DateTime LogDateTime { get; set; }
        }


        private static int CalculateWorkedMinutes(
    IReadOnlyCollection<ResolvedAttendanceEvent> events)
        {
            var orderedEvents =
                events
                    .Where(x =>
                        x.ClockType ==
                            AttendanceClockType.CheckIn
                        ||
                        x.ClockType ==
                            AttendanceClockType.CheckOut)
                    .OrderBy(x => x.LogDateTime)
                    .ToList();


            if (orderedEvents.Count == 0)
                return 0;


            DateTime? currentCheckIn = null;

            var totalWorkedMinutes = 0;


            foreach (var attendanceEvent in orderedEvents)
            {
                // ========================================================
                // CHECK IN
                // ========================================================

                if (attendanceEvent.ClockType ==
                    AttendanceClockType.CheckIn)
                {
                    // Only open a working period when we are currently OUT.
                    //
                    // Duplicate CheckIns do not restart the period.
                    if (!currentCheckIn.HasValue)
                    {
                        currentCheckIn =
                            attendanceEvent.LogDateTime;
                    }

                    continue;
                }


                // ========================================================
                // CHECK OUT
                // ========================================================

                if (attendanceEvent.ClockType ==
                    AttendanceClockType.CheckOut)
                {
                    // Cannot close a period if we were not IN.
                    if (!currentCheckIn.HasValue)
                        continue;


                    if (attendanceEvent.LogDateTime >
                        currentCheckIn.Value)
                    {
                        totalWorkedMinutes +=
                            CalculateMinutes(
                                currentCheckIn.Value,
                                attendanceEvent.LogDateTime);
                    }


                    // Employee is now OUT.
                    currentCheckIn = null;
                }
            }


            return totalWorkedMinutes;
        }



    }
}