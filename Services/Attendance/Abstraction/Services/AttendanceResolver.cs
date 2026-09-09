using HRM.DTOs.Attendance;
using HRM.Enum;

namespace HRM.Services.Attendance.Abstraction.Services
{
    public class AttendanceResolver : IAttendanceResolver
    {
        public Task<List<AttendanceSegmentResultDto>> ResolveAsync(
        AttendanceWorkPlanDto plan,
        List<AttendanceRawLogDto> logs,
        CancellationToken cancellationToken = default)
        {
            var results =
                new List<AttendanceSegmentResultDto>();

            var unusedLogs =
                logs
                    .OrderBy(x => x.LogDateTime)
                    .ToList();

            var attendanceSegments =
                plan.Segments
                    .Where(x => x.RequiresAttendance)
                    .OrderBy(x => x.SequenceNumber)
                    .ToList();

            foreach (var segment in attendanceSegments)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result =
                    new AttendanceSegmentResultDto
                    {
                        WorkPlanSegmentId =
                            segment.WorkPlanSegmentId,

                        SegmentName =
                            segment.Name,

                        PlannedStart =
                            segment.StartDateTime,

                        PlannedEnd =
                            segment.EndDateTime
                    };

                // -----------------------------------------
                // 1. Find nearest unused punch to START
                // -----------------------------------------

                var checkInLog =
                    FindNearestLog(
                        unusedLogs,
                        segment.StartDateTime,
                        segment.GraceBeforeMinutes,
                        120);

                if (checkInLog != null)
                {
                    result.CheckIn =
                        new AttendanceResolvedLogDto
                        {
                            AttendanceLogId =
                                checkInLog.AttendanceLogId,

                            IndividualId =
                                checkInLog.IndividualId,

                            LogDateTime =
                                checkInLog.LogDateTime,

                            WorkPlanSegmentId =
                                segment.WorkPlanSegmentId,

                            SegmentName =
                                segment.Name,

                            ClockType =
                                AttendanceClockType.CheckIn,

                            DistanceMinutes =
                                Math.Abs(
                                    (checkInLog.LogDateTime -
                                     segment.StartDateTime)
                                    .TotalMinutes)
                        };

                    unusedLogs.Remove(
                        checkInLog);
                }

                // -----------------------------------------
                // 2. Find nearest unused punch to END
                // -----------------------------------------

                var checkOutLog =
                    FindNearestLog(
                        unusedLogs,
                        segment.EndDateTime,
                        120,
                        segment.GraceAfterMinutes);

                if (checkOutLog != null)
                {
                    result.CheckOut =
                        new AttendanceResolvedLogDto
                        {
                            AttendanceLogId =
                                checkOutLog.AttendanceLogId,

                            IndividualId =
                                checkOutLog.IndividualId,

                            LogDateTime =
                                checkOutLog.LogDateTime,

                            WorkPlanSegmentId =
                                segment.WorkPlanSegmentId,

                            SegmentName =
                                segment.Name,

                            ClockType =
                                AttendanceClockType.CheckOut,

                            DistanceMinutes =
                                Math.Abs(
                                    (checkOutLog.LogDateTime -
                                     segment.EndDateTime)
                                    .TotalMinutes)
                        };

                    unusedLogs.Remove(
                        checkOutLog);
                }

                // -----------------------------------------
                // 3. Exceptions
                // -----------------------------------------

                if (result.CheckIn == null &&
                    result.CheckOut == null)
                {
                    result.Exceptions.Add(
                        AttendanceExceptionType
                            .MissingAttendance);
                }
                else
                {
                    if (result.CheckIn == null)
                    {
                        result.Exceptions.Add(
                            AttendanceExceptionType
                                .NoCheckIn);
                    }

                    if (result.CheckOut == null)
                    {
                        result.Exceptions.Add(
                            AttendanceExceptionType
                                .NoCheckOut);
                    }
                }

                results.Add(
                    result);
            }

            return Task.FromResult(
                results);
        }

        private static AttendanceRawLogDto? FindNearestLog(
    IEnumerable<AttendanceRawLogDto> logs,
    DateTime targetTime,
    int allowedBeforeMinutes,
    int allowedAfterMinutes)
        {
            var from =
                targetTime.AddMinutes(
                    -allowedBeforeMinutes);

            var to =
                targetTime.AddMinutes(
                    allowedAfterMinutes);

            return logs
                .Where(x =>
                    x.LogDateTime >= from &&
                    x.LogDateTime <= to)
                .OrderBy(x =>
                    Math.Abs(
                        (x.LogDateTime - targetTime)
                        .TotalMinutes))
                .FirstOrDefault();
        }
    }
}
