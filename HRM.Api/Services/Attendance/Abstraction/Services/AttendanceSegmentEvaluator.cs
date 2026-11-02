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
                        WorkSegmentTypeCode = x.WorkSegmentType.Code,
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

                var code = segment.WorkSegmentTypeCode?.Trim().ToUpperInvariant() ?? "";
                var isCheckInOnly = code is "CHECK_IN" or "DUTY_CHECK_IN" or "BREAK_END";
                var isCheckOutOnly = code is "CHECK_OUT" or "DUTY_CHECK_OUT" or "BREAK_START";

                if (!isCheckOutOnly)
                {
                    EvaluateCheckIn(
                        result,
                        segment.StartDateTime,
                        segment.GraceAfterMinutes);
                }

                if (!isCheckInOnly)
                {
                    EvaluateCheckOut(
                        result,
                        isCheckOutOnly ? segment.StartDateTime : segment.EndDateTime,
                        segment.GraceBeforeMinutes);
                }

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

            var lateMinutes = HRM.Helpers.AttendanceTiming.LateMinutes(result.CheckInTime.Value, allowedCheckIn);
            if (lateMinutes > 0)
            {
                result.LateMinutes = lateMinutes;

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
