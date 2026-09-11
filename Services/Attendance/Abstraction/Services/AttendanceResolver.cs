using HRM.DTOs.Attendance;
using HRM.Enum;
using HRM.Models;

namespace HRM.Services.Attendance.Abstraction.Services
{

    public class AttendanceResolver : IAttendanceResolver
    {

        private const int CheckInResolutionBeforeMinutes = 120;
        private const int CheckOutResolutionAfterMinutes = 120;
        


        public Task<List<AttendanceResolvedLogDto>> ResolveAsync(
        AttendanceWorkPlanDto plan,
        List<AttendanceRawLogDto> logs,
        CancellationToken cancellationToken = default)
        {
            var results =
                new List<AttendanceResolvedLogDto>();

            if (plan == null)
            {
                throw new InvalidOperationException(
                    $"No attendance work plan found");
            }

            var segments =
                plan.Segments
                    .Where(x => x.RequiresAttendance)
                    .OrderBy(x => x.SequenceNumber)
                    .ToList();




            foreach (var log in logs.OrderBy(x => x.LogDateTime))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var resolved =
                    ResolveLog(
                        log,
                        segments);

                results.Add(resolved);
            }

            return Task.FromResult(results);
        }






        //private static AttendanceResolvedLogDto ResolveLog(
        //    AttendanceRawLogDto log,
        //    List<AttendanceWorkSegmentDto> segments)
        //{
        //    AttendanceWorkSegmentDto? bestSegment = null;

        //    AttendanceClockType bestType =
        //        AttendanceClockType.Unresolved;

        //    double bestDistance =
        //        double.MaxValue;


        //    foreach (var segment in segments)
        //    {
        //        var distanceToStart =
        //            Math.Abs(
        //                (log.LogDateTime -
        //                 segment.StartDateTime)
        //                .TotalMinutes);

        //        var distanceToEnd =
        //            Math.Abs(
        //                (log.LogDateTime -
        //                 segment.EndDateTime)
        //                .TotalMinutes);


        //        // -------------------------------
        //        // Candidate: Check In
        //        // -------------------------------

        //        if (distanceToStart < bestDistance)
        //        {
        //            bestDistance =
        //                distanceToStart;

        //            bestSegment =
        //                segment;

        //            bestType =
        //                AttendanceClockType.CheckIn;
        //        }


        //        // -------------------------------
        //        // Candidate: Check Out
        //        // -------------------------------

        //        if (distanceToEnd < bestDistance)
        //        {
        //            bestDistance =
        //                distanceToEnd;

        //            bestSegment =
        //                segment;

        //            bestType =
        //                AttendanceClockType.CheckOut;
        //        }
        //    }


        //    if (bestSegment == null)
        //    {
        //        return new AttendanceResolvedLogDto
        //        {
        //            AttendanceLogId =
        //                log.AttendanceLogId,

        //            IndividualId =
        //                log.IndividualId,

        //            LogDateTime =
        //                log.LogDateTime,

        //            ClockType =
        //                AttendanceClockType.Unresolved
        //        };
        //    }


        //    return new AttendanceResolvedLogDto
        //    {
        //        AttendanceLogId =
        //            log.AttendanceLogId,

        //        IndividualId =
        //            log.IndividualId,

        //        LogDateTime =
        //            log.LogDateTime,

        //        WorkPlanSegmentId =
        //            bestSegment.WorkPlanSegmentId,

        //        SegmentName =
        //            bestSegment.Name,

        //        ClockType =
        //            bestType,

        //        DistanceMinutes =
        //            bestDistance
        //    };
        //}


        private static AttendanceResolvedLogDto ResolveLog(
            AttendanceRawLogDto log,
            List<AttendanceWorkSegmentDto> segments)
        {
            var candidates =
                new List<AttendanceBoundaryCandidate>();

            foreach (var segment in segments)
            {
                var midpoint =
                    segment.StartDateTime.AddTicks(
                        (segment.EndDateTime -
                         segment.StartDateTime).Ticks / 2);


                // ========================================
                // CHECK IN WINDOW
                // ========================================

                var checkInFrom =
                     segment.StartDateTime.AddMinutes(
                         -CheckInResolutionBeforeMinutes);

                var checkInTo =
                    midpoint;

                if (log.LogDateTime >= checkInFrom &&
                    log.LogDateTime <= checkInTo)
                {
                    candidates.Add(
                        new AttendanceBoundaryCandidate
                        {
                            Segment =
                                segment,

                            ClockType =
                                AttendanceClockType.CheckIn,

                            TargetTime =
                                segment.StartDateTime,

                            DistanceMinutes =
                                Math.Abs(
                                    (log.LogDateTime -
                                     segment.StartDateTime)
                                    .TotalMinutes)
                        });
                }


                // ========================================
                // CHECK OUT WINDOW
                // ========================================

                var checkOutFrom =
                    midpoint;

                var checkOutTo =
                    segment.EndDateTime.AddMinutes(
                        CheckOutResolutionAfterMinutes);

                if (log.LogDateTime >= checkOutFrom &&
                    log.LogDateTime <= checkOutTo)
                {
                    candidates.Add(
                        new AttendanceBoundaryCandidate
                        {
                            Segment =
                                segment,

                            ClockType =
                                AttendanceClockType.CheckOut,

                            TargetTime =
                                segment.EndDateTime,

                            DistanceMinutes =
                                Math.Abs(
                                    (log.LogDateTime -
                                     segment.EndDateTime)
                                    .TotalMinutes)
                        });
                }
            }


            // Nothing matched a valid range.
            if (candidates.Count == 0)
            {
                return new AttendanceResolvedLogDto
                {
                    AttendanceLogId =
                        log.AttendanceLogId,

                    IndividualId =
                        log.IndividualId,

                    LogDateTime =
                        log.LogDateTime,

                    ClockType =
                        AttendanceClockType.Unresolved
                };
            }


            // If more than one segment/window matches,
            // choose the nearest actual boundary.
            var best =
                candidates
                    .OrderBy(x =>
                        x.DistanceMinutes)
                    .First();


            return new AttendanceResolvedLogDto
            {
                AttendanceLogId =
                    log.AttendanceLogId,

                IndividualId =
                    log.IndividualId,

                LogDateTime =
                    log.LogDateTime,

                WorkPlanSegmentId =
                    best.Segment.WorkPlanSegmentId,

                SegmentName =
                    best.Segment.Name,

                ClockType =
                    best.ClockType,

                DistanceMinutes =
                    best.DistanceMinutes
            };
        }


        private sealed class AttendanceBoundaryCandidate
        {
            public AttendanceWorkSegmentDto Segment { get; set; }
                = null!;

            public AttendanceClockType ClockType { get; set; }

            public DateTime TargetTime { get; set; }

            public double DistanceMinutes { get; set; }
        }








    }
}
