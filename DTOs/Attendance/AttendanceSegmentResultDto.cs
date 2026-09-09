using HRM.Enum;

namespace HRM.DTOs.Attendance
{
    public class AttendanceSegmentResultDto
    {
        public int WorkPlanSegmentId { get; set; }

        public string SegmentName { get; set; } = string.Empty;

        public DateTime PlannedStart { get; set; }

        public DateTime PlannedEnd { get; set; }

        public DateTime? ActualCheckIn { get; set; }

        public DateTime? ActualCheckOut { get; set; }

        public int WorkedMinutes { get; set; }

        public int LateMinutes { get; set; }

        public int EarlyLeaveMinutes { get; set; }

        public List<AttendanceExceptionType> Exceptions { get; set; } = [];


        public AttendanceResolvedLogDto? CheckIn { get; set; }

        public AttendanceResolvedLogDto? CheckOut { get; set; }

 


    }
}
