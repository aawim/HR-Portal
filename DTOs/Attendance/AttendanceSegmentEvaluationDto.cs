using HRM.Enum;

namespace HRM.DTOs.Attendance
{
    public sealed class AttendanceSegmentEvaluationDto
    {
        public int WorkPlanId { get; set; }

        public int WorkPlanSegmentId { get; set; }

        public string SegmentName { get; set; } = string.Empty;

        public DateTime PlannedStart { get; set; }

        public DateTime PlannedEnd { get; set; }

        public DateTime? CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        public int LateMinutes { get; set; }

        public int EarlyDepartureMinutes { get; set; }

        public List<AttendanceExceptionType> Exceptions { get; set; } = [];

        public bool HasExceptions =>
            Exceptions.Count > 0;
    }
}
