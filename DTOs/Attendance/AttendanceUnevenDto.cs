namespace HRM.DTOs.Attendance
{
    public class AttendanceUnevenDto
    {
        public DateTime WorkDate { get; set; }

        public long WorkPlanId { get; set; }

        public int? AttendanceLogId { get; set; }

        public int? WorkPlanSegmentId { get; set; }

        public string SegmentName { get; set; }
            = string.Empty;

        public DateTime? ExpectedFrom { get; set; }

        public DateTime? ExpectedTo { get; set; }

        public DateTime? ActualClockTime { get; set; }

        public string Reason { get; set; }
            = string.Empty;
    }
}
