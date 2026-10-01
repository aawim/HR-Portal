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
        private const int MaximumResolutionDistanceMinutes = 120;
        private readonly IWorkPlanGenerator _workPlanGenerator;
 

        public AttendanceWorkPlanResolver(
            IDbContextFactory<HrmTeContext> dbFactory, 
            IWorkPlanGenerator workPlanGenerator
            )
        {
            _dbFactory = dbFactory;
            _workPlanGenerator = workPlanGenerator;
        }
        //public async Task<AttendancePlanResolutionResult> ResolveAsync(
        //   int individualId,
        //   DateTime clockTime,
        //   CancellationToken cancellationToken = default)
        //{
        //    await using var db =
        //        await _dbFactory.CreateDbContextAsync(
        //            cancellationToken);

        //    var date = clockTime.Date;
        //    var nextDate = date.AddDays(1);

        //    var workPlan =
        //        await db.WorkPlans
        //            .AsNoTracking()
        //            .Where(x =>
        //                x.IndividualId == individualId &&
        //                x.WorkDate >= date &&
        //                x.WorkDate < nextDate &&
        //                x.IsValid)
        //            .OrderByDescending(x => x.Version)
        //            .ThenByDescending(x => x.CreatedDate)
        //            .Select(x => new
        //            {
        //                x.WorkPlanId,
        //                x.JobId
        //            })
        //            .FirstOrDefaultAsync(
        //                cancellationToken);

        //    if (workPlan == null)
        //    {
        //        return new AttendancePlanResolutionResult
        //        {
        //            State =
        //                AttendancePlanResolutionState.NoWorkPlan,

        //            ClockType =
        //                AttendanceClockType.Unresolved,

        //            Message =
        //                "No work plan was found for this attendance event."
        //        };
        //    }

        //    var segments =
        //        await db.WorkPlanSegments
        //            .AsNoTracking()
        //            .Where(x =>
        //                x.WorkPlanId == workPlan.WorkPlanId &&
        //                x.IsValid &&
        //                x.RequiresAttendance)
        //            .OrderBy(x => x.SequenceNumber)
        //            .Select(x => new
        //            {
        //                x.WorkPlanSegmentId,
        //                x.Name,
        //                x.StartDateTime,
        //                x.EndDateTime
        //            })
        //            .ToListAsync(
        //                cancellationToken);

        //    if (segments.Count == 0)
        //    {
        //        return new AttendancePlanResolutionResult
        //        {
        //            WorkPlanId =
        //                workPlan.WorkPlanId,

        //            JobId =
        //                workPlan.JobId,

        //            State =
        //                AttendancePlanResolutionState.NoSegment,

        //            ClockType =
        //                AttendanceClockType.Unresolved,

        //            Message =
        //                "The work plan does not contain an attendance-required segment."
        //        };
        //    }

        //    var candidates =
        //        new List<BoundaryCandidate>();

        //    foreach (var segment in segments)
        //    {
        //        var distanceToStart =
        //            Math.Abs(
        //                (clockTime -
        //                 segment.StartDateTime)
        //                .TotalMinutes);

        //        var distanceToEnd =
        //            Math.Abs(
        //                (clockTime -
        //                 segment.EndDateTime)
        //                .TotalMinutes);

        //        candidates.Add(
        //            new BoundaryCandidate
        //            {
        //                WorkPlanSegmentId =
        //                    segment.WorkPlanSegmentId,

        //                SegmentName =
        //                    segment.Name,

        //                ClockType =
        //                    AttendanceClockType.CheckIn,

        //                BoundaryTime =
        //                    segment.StartDateTime,

        //                DistanceMinutes =
        //                    distanceToStart
        //            });

        //        candidates.Add(
        //            new BoundaryCandidate
        //            {
        //                WorkPlanSegmentId =
        //                    segment.WorkPlanSegmentId,

        //                SegmentName =
        //                    segment.Name,

        //                ClockType =
        //                    AttendanceClockType.CheckOut,

        //                BoundaryTime =
        //                    segment.EndDateTime,

        //                DistanceMinutes =
        //                    distanceToEnd
        //            });
        //    }

        //    var bestCandidate =
        //        candidates
        //            .OrderBy(x =>
        //                x.DistanceMinutes)
        //            .First();


        //    if (bestCandidate.DistanceMinutes > MaximumResolutionDistanceMinutes)
        //    {
        //        return new AttendancePlanResolutionResult
        //        {
        //            WorkPlanId =
        //                workPlan.WorkPlanId,

        //            JobId =
        //                workPlan.JobId,

        //            State =
        //                AttendancePlanResolutionState.OutsideResolutionWindow,

        //            ClockType =
        //                AttendanceClockType.Unresolved,

        //            DistanceMinutes =
        //                bestCandidate.DistanceMinutes,

        //            Message =
        //                $"Attendance event at {clockTime:HH:mm} " +
        //                $"is outside the permitted resolution window."
        //        };
        //    }



        //    return new AttendancePlanResolutionResult
        //    {
        //        WorkPlanId =
        //            workPlan.WorkPlanId,

        //                    WorkPlanSegmentId =
        //            bestCandidate.WorkPlanSegmentId,

        //                    JobId =
        //            workPlan.JobId,

        //                    SegmentName =
        //            bestCandidate.SegmentName,

        //                    ClockType =
        //            bestCandidate.ClockType,

        //                    State =
        //            AttendancePlanResolutionState.Resolved,

        //                    DistanceMinutes =
        //            bestCandidate.DistanceMinutes,

        //        Message =
        //            $"Attendance event resolved to " +
        //            $"'{bestCandidate.SegmentName}' as " +
        //            $"{bestCandidate.ClockType}."



        //    };
        //}

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
                       x.SequenceNumber,

                       WorkSegmentTypeCode =
                           x.WorkSegmentType.Code,

                       x.StartDateTime,
                       x.EndDateTime,

                       x.RequiresAttendance,
                       x.GraceBeforeMinutes,
                       x.GraceAfterMinutes
                   })
                   .ToListAsync(cancellationToken);


            var containingSegment =
    segments
        .Where(x =>
            IsNonBoundarySegment(
                x.WorkSegmentTypeCode))
        .Where(x =>
            clockTime >= x.StartDateTime &&
            clockTime <= x.EndDateTime)
        .OrderBy(x => x.SequenceNumber)
        .FirstOrDefault();

            if (containingSegment != null)
            {
                return new AttendancePlanResolutionResult
                {
                    WorkPlanId =
                        workPlanId,

                    WorkPlanSegmentId =
                        containingSegment.WorkPlanSegmentId,

                    JobId =
                        jobId,

                    SegmentName =
                        containingSegment.Name,

                    ClockType =
                        AttendanceClockType.Ignored,

                    State =
                        AttendancePlanResolutionState.Resolved,

                    BoundaryTime =
                        clockTime,

                    DistanceMinutes =
                        0,

                    Message =
                        $"Attendance event occurred during " +
                        $"'{containingSegment.Name}'."
                };
            }


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
                        "The work plan does not contain an " +
                        "attendance-required segment."
                };
            }

            var candidates =
      new List<BoundaryCandidate>();

            foreach (var segment in segments)
            {
                var code =
                    segment.WorkSegmentTypeCode?
                        .Trim()
                        .ToUpperInvariant();


                // =========================================================
                // CHECK-IN BOUNDARIES
                // =========================================================

                if (IsCheckInBoundary(code))
                {
                    var boundaryTime =
                        segment.StartDateTime;

                    var distance =
                        Math.Abs(
                            (clockTime - boundaryTime)
                            .TotalMinutes);

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
                                boundaryTime,

                            DistanceMinutes =
                                distance
                        });
                }


                // =========================================================
                // CHECK-OUT BOUNDARIES
                // =========================================================

                if (IsCheckOutBoundary(code))
                {
                    var boundaryTime =
                        segment.EndDateTime;

                    var distance =
                        Math.Abs(
                            (clockTime - boundaryTime)
                            .TotalMinutes);

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
                                boundaryTime,

                            DistanceMinutes =
                                distance
                        });
                }
            }
            if (candidates.Count == 0)
            {
                return new AttendancePlanResolutionResult
                {
                    WorkPlanId =
                        workPlanId,

                    JobId =
                        jobId,

                    State =
                        AttendancePlanResolutionState.NoSegment,

                    ClockType =
                        AttendanceClockType.Unresolved,

                    Message =
                        "The work plan does not contain an applicable " +
                        "attendance clock boundary."
                };
            }


            var bestCandidate =
                candidates
                    .OrderBy(x => x.DistanceMinutes)
                    .First();



            if (bestCandidate.DistanceMinutes >
                MaximumResolutionDistanceMinutes)
            {
                return new AttendancePlanResolutionResult
                {
                    WorkPlanId = workPlanId,
                    JobId = jobId,

                    State =
                        AttendancePlanResolutionState
                            .OutsideResolutionWindow,

                    ClockType =
                        AttendanceClockType.Unresolved,

                    BoundaryTime =
                        bestCandidate.BoundaryTime,

                    DistanceMinutes =
                        bestCandidate.DistanceMinutes,

                    Message =
                        $"Attendance event at {clockTime:HH:mm} " +
                        $"is outside the permitted resolution window."
                };
            }

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

        private static bool IsCheckInBoundary(
        string? code)
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


        private static bool IsCheckOutBoundary(
            string? code)
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


        private static bool IsNonBoundarySegment(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            return code.Trim().ToUpperInvariant() switch
            {
                "BREAK" => true,
                "LUNCH" => true,
                "MEETING" => true,
                "TRAINING" => true,
                "TRAVEL" => true,
                "ON_CALL" => true,
                "INFORMATIONAL" => true,

                _ => false
            };
        }


        private sealed class BoundaryCandidate
        {
            public int WorkPlanSegmentId { get; set; }

            public string SegmentName { get; set; }
                = string.Empty;

            public AttendanceClockType ClockType { get; set; }

            public DateTime BoundaryTime { get; set; }

            public double DistanceMinutes { get; set; }
        }

    }
}
