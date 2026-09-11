using HRM.Constants;
using HRM.DTOs.Attendance;
using HRM.Enum;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Attendance.Abstraction.Services
{
    public sealed class AttendanceSegmentEvaluator
     : IAttendanceSegmentEvaluator
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;

        public AttendanceSegmentEvaluator(
            IDbContextFactory<HrmTeContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<AttendanceSegmentEvaluationDto>> EvaluateAsync(
            int workPlanId,
            CancellationToken cancellationToken = default)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync(cancellationToken);

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
                        x.EndDateTime,
                        x.GraceBeforeMinutes,
                        x.GraceAfterMinutes
                    })
                    .ToListAsync(cancellationToken);

            var resolutions =
                await db.AttendanceLogResolutions
                    .AsNoTracking()
                    .Include(x => x.AttendanceLog)
                    .Where(x =>
                        x.WorkPlanId == workPlanId &&
                        x.IsValid &&
                        x.AttendanceResolutionStatusId ==
                            AttendanceResolutionStatusIds.Resolved)
                    .ToListAsync(cancellationToken);

            var results =
                new List<AttendanceSegmentEvaluationDto>();

            foreach (var segment in segments)
            {
                var checkIn =
                    resolutions.FirstOrDefault(x =>
                        x.WorkPlanSegmentId ==
                            segment.WorkPlanSegmentId &&
                        x.AttendanceClockTypeId ==
                            (int)AttendanceClockType.CheckIn);

                var checkOut =
                    resolutions.FirstOrDefault(x =>
                        x.WorkPlanSegmentId ==
                            segment.WorkPlanSegmentId &&
                        x.AttendanceClockTypeId ==
                            (int)AttendanceClockType.CheckOut);

                var result =
                    new AttendanceSegmentEvaluationDto
                    {
                        WorkPlanId = workPlanId,

                        WorkPlanSegmentId =
                            segment.WorkPlanSegmentId,

                        SegmentName =
                            segment.Name,

                        PlannedStart =
                            segment.StartDateTime,

                        PlannedEnd =
                            segment.EndDateTime,

                        CheckInTime =
                            checkIn?.AttendanceLog.Date,

                        CheckOutTime =
                            checkOut?.AttendanceLog.Date
                    };

                EvaluateCheckIn(
                    result,
                    segment.StartDateTime,
                    segment.GraceAfterMinutes);

                EvaluateCheckOut(
                    result,
                    segment.EndDateTime,
                    segment.GraceBeforeMinutes);

                results.Add(result);
            }

            return results;
        }

        private static void EvaluateCheckIn(
            AttendanceSegmentEvaluationDto result,
            DateTime plannedStart,
            int graceMinutes)
        {
            if (!result.CheckInTime.HasValue)
            {
                result.Exceptions.Add(
                    AttendanceExceptionType.NoCheckIn);

                return;
            }

            var allowedCheckIn =
                plannedStart.AddMinutes(graceMinutes);

            if (result.CheckInTime.Value > allowedCheckIn)
            {
                result.LateMinutes =
                    (int)Math.Ceiling(
                        (result.CheckInTime.Value - plannedStart)
                        .TotalMinutes);

                result.Exceptions.Add(
                    AttendanceExceptionType.LateCheckIn);
            }
        }

        private static void EvaluateCheckOut(
            AttendanceSegmentEvaluationDto result,
            DateTime plannedEnd,
            int graceMinutes)
        {
            if (!result.CheckOutTime.HasValue)
            {
                result.Exceptions.Add(
                    AttendanceExceptionType.NoCheckOut);

                return;
            }

            var allowedCheckOut =
                plannedEnd.AddMinutes(-graceMinutes);

            if (result.CheckOutTime.Value < allowedCheckOut)
            {
                result.EarlyDepartureMinutes =
                    (int)Math.Ceiling(
                        (plannedEnd - result.CheckOutTime.Value)
                        .TotalMinutes);

                result.Exceptions.Add(
                    AttendanceExceptionType.EarlyCheckOut);
            }
        }
    }

}
