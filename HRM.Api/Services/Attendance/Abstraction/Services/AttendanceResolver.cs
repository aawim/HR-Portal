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


        private static AttendanceResolvedLogDto ResolveLog(
            AttendanceRawLogDto log,
            List<AttendanceWorkSegmentDto> segments)
        {
            var candidates =
                new List<AttendanceBoundaryCandidate>();

            foreach (var segment in segments)
            {
                var code = segment.WorkSegmentTypeCode?.Trim().ToUpperInvariant() ?? "";
                var isCheckInOnly = code is "CHECK_IN" or "DUTY_CHECK_IN" or "BREAK_END";
                var isCheckOutOnly = code is "CHECK_OUT" or "DUTY_CHECK_OUT" or "BREAK_START";
                var midpoint =
                    segment.StartDateTime.AddTicks(
                        (segment.EndDateTime -
                         segment.StartDateTime).Ticks / 2);


                // ========================================
                // CHECK IN WINDOW
                // ========================================

                if (!isCheckOutOnly)
                {
                    var checkInFrom =
                         segment.StartDateTime.AddMinutes(
                             -CheckInResolutionBeforeMinutes);

                    var checkInTo =
                        isCheckInOnly ? segment.EndDateTime : midpoint;

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
                }


                // ========================================
                // CHECK OUT WINDOW
                // ========================================

                if (!isCheckInOnly)
                {
                    var expectedCheckOut =
                        isCheckOutOnly ? segment.StartDateTime : segment.EndDateTime;

                    var checkOutFrom =
                        isCheckOutOnly
                            ? segment.StartDateTime.AddMinutes(-segment.GraceBeforeMinutes - CheckInResolutionBeforeMinutes)
                            : midpoint;

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
                                    expectedCheckOut,

                                DistanceMinutes =
                                    Math.Abs(
                                        (log.LogDateTime -
                                         expectedCheckOut)
                                        .TotalMinutes)
                            });
                    }
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
