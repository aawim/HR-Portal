using HRM.Enum;

namespace HRM.DTOs.Attendance
{
    public class AttendanceResolvedLogDto
    {
        public int AttendanceLogId { get; set; }

        public int IndividualId { get; set; }

        public DateTime LogDateTime { get; set; }

        public int? WorkPlanSegmentId { get; set; }

        public string? SegmentName { get; set; }

        public AttendanceClockType ClockType { get; set; }

        public double DistanceMinutes { get; set; }
    }
}
