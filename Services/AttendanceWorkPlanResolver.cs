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
                        x.IsValid &&
                        x.RequiresAttendance)
                    .OrderBy(x => x.SequenceNumber)
                    .Select(x => new
                    {
                        x.WorkPlanSegmentId,
                        x.Name,
                        x.StartDateTime,
                        x.EndDateTime
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
                        "The work plan does not contain an " +
                        "attendance-required segment."
                };
            }

            var candidates =
                new List<BoundaryCandidate>();

            foreach (var segment in segments)
            {
                var distanceToStart =
                    Math.Abs(
                        (clockTime - segment.StartDateTime)
                        .TotalMinutes);

                var distanceToEnd =
                    Math.Abs(
                        (clockTime - segment.EndDateTime)
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
                            segment.StartDateTime,

                        DistanceMinutes =
                            distanceToStart
                    });

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
                            segment.EndDateTime,

                        DistanceMinutes =
                            distanceToEnd
                    });
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
