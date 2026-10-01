using HRM.Constants;
using HRM.Data;
using HRM.DTOs.Attendance;
using HRM.Enum;
using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.Services.Attendance.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Attendance.Evaluation
{
    public sealed class AttendanceEvaluationService : IAttendanceEvaluationService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;

        public AttendanceEvaluationService(
            IDbContextFactory<HrmTeContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }


        // ============================================================
        // DAILY EVALUATION
        // ============================================================

        public async Task<AttendanceDailyEvaluationDto> EvaluateAsync(
            int individualId,
            int jobId,
            DateTime workDate,
            CancellationToken cancellationToken = default)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var date = workDate.Date;
            var evaluationTime = DateTime.Now;


            // ========================================================
            // LOAD WORK PLAN
            // ========================================================

            var workPlan = await db.WorkPlans
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

                .FirstOrDefaultAsync(cancellationToken);


            // ========================================================
            // NO WORK PLAN
            // ========================================================

            if (workPlan == null)
            {
                return new AttendanceDailyEvaluationDto
                {
                    IndividualId = individualId,
                    JobId = jobId,
                    WorkDate = date,

                    HasWorkPlan = false,
                    RequiresAttendance = false,
                    RequiresCheckOut = false,

                    Status =
                        date > DateTime.Today
                            ? AttendanceDailyStatus.Future
                            : AttendanceDailyStatus.Unknown,

                    IsComplete = false,
                    HasException = false
                };
            }


            // ========================================================
            // VALID SEGMENTS
            // ========================================================

            var segments = workPlan.WorkPlanSegments
                .Where(x => x.IsValid)
                .OrderBy(x => x.SequenceNumber)
                .ThenBy(x => x.StartDateTime)
                .ToList();


            // These are segments that participate in physical
            // attendance processing.
            var attendanceSegments = segments
                .Where(x => x.RequiresAttendance)
                .ToList();


            // ========================================================
            // RESOLVED ATTENDANCE EVENTS
            // ========================================================

            var resolvedEvents =
                workPlan.AttendanceLogResolutions
                    .Where(x => x.JobId == jobId)

                    .Where(x =>
                        x.WorkPlanId.HasValue &&
                        x.WorkPlanSegmentId.HasValue &&
                        x.AttendanceClockTypeId.HasValue)

                    .Where(x =>
                        x.AttendanceLog != null)

                    .Where(x =>
                        x.AttendanceResolutionStatusId ==
                        AttendanceResolutionStatusIds.Resolved)

                    .Where(x =>
                        x.AttendanceClockTypeId ==
                            (int)AttendanceClockType.CheckIn ||
                        x.AttendanceClockTypeId ==
                            (int)AttendanceClockType.CheckOut)

                    .Select(x =>
                        new ResolvedAttendanceEvent
                        {
                            AttendanceLogId =
                                x.AttendanceLogId,

                            AttendanceLogResolutionId =
                                x.AttendanceLogResolutionId,

                            WorkPlanId =
                                x.WorkPlanId!.Value,

                            WorkPlanSegmentId =
                                x.WorkPlanSegmentId!.Value,

                            ClockType =
                                (AttendanceClockType)
                                x.AttendanceClockTypeId!.Value,

                            LogDateTime =
                                x.AttendanceLog.Date
                        })

                    .OrderBy(x => x.LogDateTime)
                    .ToList();


            // ========================================================
            // RESOLUTION COUNTS
            // ========================================================

            var allResolutions =
                workPlan.AttendanceLogResolutions
                    .Where(x => x.JobId == jobId)
                    .ToList();


            var rawClockCount =
                allResolutions
                    .Select(x => x.AttendanceLogId)
                    .Distinct()
                    .Count();


            var resolvedClockCount =
                resolvedEvents.Count;


            var unresolvedClockCount =
                allResolutions.Count(x =>
                    x.AttendanceClockTypeId ==
                    (int)AttendanceClockType.Unresolved);


            // ========================================================
            // EVALUATE SEGMENTS
            // ========================================================

            var evaluatedSegments =
                new List<AttendanceEvaluatedSegmentDto>();


            foreach (var segment in segments)
            {
                var evaluated =
                    EvaluateSegment(
                        segment,
                        resolvedEvents,
                        evaluationTime);

                evaluatedSegments.Add(evaluated);
            }


            // ========================================================
            // ONLY ATTENDANCE-REQUIRED SEGMENTS
            // ========================================================

            var requiredEvaluations =
                evaluatedSegments
                    .Where(x => x.RequiresAttendance)
                    .ToList();


            var requiresAttendance =
                attendanceSegments.Count > 0;


            // Does the day contain an actual checkout boundary?
            var requiresCheckOut =
                attendanceSegments.Any(x =>
                    RequiresCheckOut(
                        x.WorkSegmentType?.Code));


            // ========================================================
            // PLANNED TIMES
            // ========================================================

            DateTime? plannedStart = null;
            DateTime? plannedEnd = null;


            if (attendanceSegments.Count > 0)
            {
                plannedStart =
                    attendanceSegments.Min(
                        x => x.StartDateTime);

                plannedEnd =
                    attendanceSegments.Max(
                        x => x.EndDateTime);
            }


            // ========================================================
            // PLANNED MINUTES
            // ========================================================

            var plannedMinutes =
                attendanceSegments
                    .Where(x =>
                        IsDurationSegment(
                            x.WorkSegmentType?.Code))
                    .Sum(x =>
                        CalculateMinutes(
                            x.StartDateTime,
                            x.EndDateTime));


            // ========================================================
            // ACTUAL FIRST CHECK IN
            // ========================================================

            var checkIns =
                resolvedEvents
                    .Where(x =>
                        x.ClockType ==
                        AttendanceClockType.CheckIn)
                    .Select(x => x.LogDateTime)
                    .ToList();


            DateTime? firstCheckIn =
                checkIns.Count > 0
                    ? checkIns.Min()
                    : null;


            // ========================================================
            // ACTUAL LAST CHECK OUT
            // ========================================================

            var checkOuts =
                resolvedEvents
                    .Where(x =>
                        x.ClockType ==
                        AttendanceClockType.CheckOut)
                    .Select(x => x.LogDateTime)
                    .ToList();


            DateTime? lastCheckOut =
                checkOuts.Count > 0
                    ? checkOuts.Max()
                    : null;


            // ========================================================
            // WORKED MINUTES
            // ========================================================

            var workedMinutes = 0;


            if (firstCheckIn.HasValue &&
                lastCheckOut.HasValue &&
                lastCheckOut.Value > firstCheckIn.Value)
            {
                workedMinutes =
                    CalculateMinutes(
                        firstCheckIn.Value,
                        lastCheckOut.Value);
            }


            // ========================================================
            // LATE / EARLY
            // ========================================================

            var lateMinutes =
                evaluatedSegments.Sum(
                    x => x.LateMinutes);


            var earlyDepartureMinutes =
                evaluatedSegments.Sum(
                    x => x.EarlyDepartureMinutes);


            // ========================================================
            // ISSUES
            // ========================================================

            var issues =
                evaluatedSegments
                    .SelectMany(x => x.Issues)
                    .ToList();


            // ========================================================
            // UNRESOLVED CLOCK ISSUES
            // ========================================================

            foreach (var resolution in
                     allResolutions.Where(x =>
                         x.AttendanceClockTypeId ==
                         (int)AttendanceClockType.Unresolved))
            {
                issues.Add(
                    new AttendanceEvaluationIssueDto
                    {
                        Type =
                            AttendanceEvaluationIssueType
                                .UnresolvedClock,

                        Severity =
                            AttendanceEvaluationIssueSeverity
                                .Warning,

                        Message =
                            string.IsNullOrWhiteSpace(
                                resolution.ResolutionMessage)
                                ? "Attendance clock could not be resolved."
                                : resolution.ResolutionMessage,

                        AttendanceLogId =
                            resolution.AttendanceLogId,

                        ActualTime =
                            resolution.AttendanceLog?.Date
                    });
            }


            // ========================================================
            // COMPLETION
            // ========================================================

            var isComplete =
                !requiresAttendance ||
                requiredEvaluations.All(
                    x => x.IsComplete);


            var hasException =
                issues.Count > 0;


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
                    requiresCheckOut,

                PlannedStart =
                    plannedStart,

                PlannedEnd =
                    plannedEnd,

                PlannedMinutes =
                    plannedMinutes,

                FirstCheckIn =
                    firstCheckIn,

                LastCheckOut =
                    lastCheckOut,

                WorkedMinutes =
                    workedMinutes,

                LateMinutes =
                    lateMinutes,

                EarlyDepartureMinutes =
                    earlyDepartureMinutes,

                // Do not calculate approved OT here.
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
        // SEGMENT EVALUATION
        // ============================================================

        private static AttendanceEvaluatedSegmentDto EvaluateSegment(
            WorkPlanSegment segment,
            IReadOnlyCollection<ResolvedAttendanceEvent> events,
            DateTime evaluationTime)
        {
            var code =
                segment.WorkSegmentType?.Code?
                    .Trim()
                    .ToUpperInvariant();


            var requiresCheckIn =
                RequiresCheckIn(code);


            var requiresCheckOut =
                RequiresCheckOut(code);


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
                        IsDurationSegment(code)
                            ? CalculateMinutes(
                                segment.StartDateTime,
                                segment.EndDateTime)
                            : 0
                };


            // ========================================================
            // SEGMENT DOES NOT REQUIRE ATTENDANCE
            // ========================================================

            if (!segment.RequiresAttendance)
            {
                result.IsComplete = true;
                result.HasException = false;

                return result;
            }


            // ========================================================
            // EVENTS BELONGING TO THIS SEGMENT
            // ========================================================

            var segmentEvents =
                events
                    .Where(x =>
                        x.WorkPlanSegmentId ==
                        segment.WorkPlanSegmentId)

                    .OrderBy(x => x.LogDateTime)
                    .ToList();


            // ========================================================
            // CHECK IN
            // ========================================================

            var checkInEvent =
                segmentEvents
                    .Where(x =>
                        x.ClockType ==
                        AttendanceClockType.CheckIn)
                    .OrderBy(x => x.LogDateTime)
                    .FirstOrDefault();


            // ========================================================
            // CHECK OUT
            // ========================================================

            var checkOutEvent =
                segmentEvents
                    .Where(x =>
                        x.ClockType ==
                        AttendanceClockType.CheckOut)
                    .OrderByDescending(x => x.LogDateTime)
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
            // CHECK-IN BOUNDARY
            // ========================================================

            if (requiresCheckIn)
            {
                var deadline =
                    segment.StartDateTime
                        .AddMinutes(
                            segment.GraceAfterMinutes);


                if (!checkIn.HasValue)
                {
                    // Only become missing once the allowed
                    // check-in window has passed.
                    if (evaluationTime > deadline)
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
                                    segment.StartDateTime
                            });
                    }
                }
                else
                {
                    // -----------------------------------------------
                    // LATE CHECK IN
                    // -----------------------------------------------

                    if (checkIn.Value > deadline)
                    {
                        result.LateMinutes =
                            CalculateMinutes(
                                deadline,
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
                                    checkInEvent?.AttendanceLogId,

                                ExpectedTime =
                                    segment.StartDateTime,

                                ActualTime =
                                    checkIn.Value,

                                DifferenceMinutes =
                                    result.LateMinutes
                            });
                    }
                }
            }


            // ========================================================
            // CHECK-OUT BOUNDARY
            // ========================================================

            if (requiresCheckOut)
            {
                var deadline =
                    segment.EndDateTime
                        .AddMinutes(
                            segment.GraceAfterMinutes);


                if (!checkOut.HasValue)
                {
                    // Do not report missing checkout while
                    // the employee is still working.
                    if (evaluationTime > deadline)
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
                                    segment.EndDateTime
                            });
                    }
                }
                else
                {
                    // -----------------------------------------------
                    // EARLY CHECK OUT
                    // -----------------------------------------------

                    var earliestAllowed =
                        segment.EndDateTime
                            .AddMinutes(
                                -segment.GraceBeforeMinutes);


                    if (checkOut.Value < earliestAllowed)
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
                                    $"{result.EarlyDepartureMinutes} " +
                                    $"minute(s).",

                                WorkPlanSegmentId =
                                    segment.WorkPlanSegmentId,

                                AttendanceLogId =
                                    checkOutEvent?.AttendanceLogId,

                                ExpectedTime =
                                    segment.EndDateTime,

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
            // COMPLETION
            // ========================================================

            if (requiresCheckIn)
            {
                var deadline =
                    segment.StartDateTime
                        .AddMinutes(
                            segment.GraceAfterMinutes);


                // Upcoming boundary should not make the
                // whole day incomplete.
                result.IsComplete =
                    checkIn.HasValue ||
                    evaluationTime <= deadline;
            }
            else if (requiresCheckOut)
            {
                var deadline =
                    segment.EndDateTime
                        .AddMinutes(
                            segment.GraceAfterMinutes);


                result.IsComplete =
                    checkOut.HasValue ||
                    evaluationTime <= deadline;
            }
            else
            {
                // WORK, WORK_PERIOD, MEETING, TRAINING,
                // TRAVEL, INFORMATIONAL, etc.
                //
                // These segments do not independently require
                // a physical clock event.
                result.IsComplete = true;
            }


            result.HasException =
                result.Issues.Count > 0;


            return result;
        }


        // ============================================================
        // DAILY STATUS
        // ============================================================

        private static AttendanceDailyStatus DetermineDailyStatus(
            DateTime workDate,
            DateTime evaluationTime,
            IReadOnlyCollection<WorkPlanSegment> attendanceSegments,
            IReadOnlyCollection<AttendanceEvaluatedSegmentDto>
                evaluatedSegments,
            IReadOnlyCollection<ResolvedAttendanceEvent> events,
            bool requiresAttendance,
            bool isComplete)
        {
            // Future calendar date
            if (workDate.Date > evaluationTime.Date)
            {
                return AttendanceDailyStatus.Future;
            }


            // No physical attendance required
            if (!requiresAttendance)
            {
                return AttendanceDailyStatus.RestDay;
            }


            var firstBoundary =
                attendanceSegments
                    .Where(x =>
                        IsClockBoundary(
                            x.WorkSegmentType?.Code))
                    .OrderBy(x => x.StartDateTime)
                    .FirstOrDefault();


            var lastBoundary =
                attendanceSegments
                    .Where(x =>
                        IsClockBoundary(
                            x.WorkSegmentType?.Code))
                    .OrderByDescending(x => x.EndDateTime)
                    .FirstOrDefault();


            // Before today's first expected attendance boundary
            if (workDate.Date == evaluationTime.Date &&
                firstBoundary != null &&
                evaluationTime < firstBoundary.StartDateTime)
            {
                return AttendanceDailyStatus.Future;
            }


            // Has employee produced any resolved physical
            // attendance event?
            var hasResolvedAttendance =
                events.Any();


            // --------------------------------------------------------
            // TODAY - STILL IN PROGRESS
            // --------------------------------------------------------

            if (workDate.Date == evaluationTime.Date &&
                lastBoundary != null)
            {
                var finalDeadline =
                    lastBoundary.EndDateTime
                        .AddMinutes(
                            lastBoundary.GraceAfterMinutes);


                if (evaluationTime <= finalDeadline)
                {
                    return AttendanceDailyStatus.InProgress;
                }
            }


            // --------------------------------------------------------
            // SHIFT/DAY FINISHED
            // --------------------------------------------------------

            if (!hasResolvedAttendance)
            {
                return AttendanceDailyStatus.Absent;
            }


            if (isComplete)
            {
                return AttendanceDailyStatus.Present;
            }


            return AttendanceDailyStatus.Incomplete;
        }


        // ============================================================
        // SEGMENT TYPE SEMANTICS
        // ============================================================

        private static bool RequiresCheckIn(
            string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;


            return code.Trim().ToUpperInvariant() switch
            {
                "CHECK_IN" => true,

                "DUTY_CHECK_IN" => true,

                // Returning from a break means the employee
                // clocks back into duty.
                "BREAK_END" => true,

                _ => false
            };
        }


        private static bool RequiresCheckOut(
            string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;


            return code.Trim().ToUpperInvariant() switch
            {
                "CHECK_OUT" => true,

                "DUTY_CHECK_OUT" => true,

                // Starting a break means the employee
                // clocks out of the active work period.
                "BREAK_START" => true,

                _ => false
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
        // DURATION SEGMENTS
        // ============================================================

        private static bool IsDurationSegment(
            string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;


            return code.Trim().ToUpperInvariant() switch
            {
                "WORK_PERIOD" => true,
                "WORK" => true,
                "BREAK" => true,
                "LUNCH" => true,
                "MEETING" => true,
                "TRAINING" => true,
                "TRAVEL" => true,
                "ON_CALL" => true,

                _ => false
            };
        }


        // ============================================================
        // MINUTE CALCULATION
        // ============================================================

        private static int CalculateMinutes(
            DateTime from,
            DateTime to)
        {
            if (to <= from)
                return 0;


            return (int)Math.Round(
                (to - from).TotalMinutes,
                MidpointRounding.AwayFromZero);
        }

 


        // ============================================================
        // INTERNAL RESOLVED EVENT
        // ============================================================

        private sealed class ResolvedAttendanceEvent
        {
            public int AttendanceLogId { get; init; }

            public long AttendanceLogResolutionId { get; init; }

            public long WorkPlanId { get; init; }

            public int WorkPlanSegmentId { get; init; }

            public AttendanceClockType ClockType { get; init; }

            public DateTime LogDateTime { get; init; }
        }
    }
}