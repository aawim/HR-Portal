using HRM.DTOs.Attendance;
using HRM.Enum;
using HRM.Models;

namespace HRM.Services.Attendance.Abstraction.Services
{

    public class AttendanceResolver : IAttendanceResolver
    {
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
            AttendanceWorkSegmentDto? bestSegment = null;

            AttendanceClockType bestType =
                AttendanceClockType.Unresolved;

            double bestDistance =
                double.MaxValue;


            foreach (var segment in segments)
            {
                var distanceToStart =
                    Math.Abs(
                        (log.LogDateTime -
                         segment.StartDateTime)
                        .TotalMinutes);

                var distanceToEnd =
                    Math.Abs(
                        (log.LogDateTime -
                         segment.EndDateTime)
                        .TotalMinutes);


                // -------------------------------
                // Candidate: Check In
                // -------------------------------

                if (distanceToStart < bestDistance)
                {
                    bestDistance =
                        distanceToStart;

                    bestSegment =
                        segment;

                    bestType =
                        AttendanceClockType.CheckIn;
                }


                // -------------------------------
                // Candidate: Check Out
                // -------------------------------

                if (distanceToEnd < bestDistance)
                {
                    bestDistance =
                        distanceToEnd;

                    bestSegment =
                        segment;

                    bestType =
                        AttendanceClockType.CheckOut;
                }
            }


            if (bestSegment == null)
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


            return new AttendanceResolvedLogDto
            {
                AttendanceLogId =
                    log.AttendanceLogId,

                IndividualId =
                    log.IndividualId,

                LogDateTime =
                    log.LogDateTime,

                WorkPlanSegmentId =
                    bestSegment.WorkPlanSegmentId,

                SegmentName =
                    bestSegment.Name,

                ClockType =
                    bestType,

                DistanceMinutes =
                    bestDistance
            };
        }

    
    }
}
