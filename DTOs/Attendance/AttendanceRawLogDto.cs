using HRM.Enum;

namespace HRM.DTOs.Attendance
{
    public class AttendanceRawLogDto
    {
        //public int AttendanceLogId { get; set; }

        //public int IndividualId { get; set; }

        //public int InOutModeId { get; set; }

        //public DateTime LogDateTime { get; set; }

        //public string Mode =>
        //    InOutModeId == 1 ? "IN" :
        //    InOutModeId == 2 ? "OUT" :
        //    "UNKNOWN";

        public int AttendanceLogId { get; set; }

        public int IndividualId { get; set; }

        public int InOutModeId { get; set; }

        public DateTime LogDateTime { get; set; }

        public int? WorkPlanSegmentId { get; set; }

        public string? SegmentName { get; set; }

   

        // What this fingerprint/clock event means
        public AttendanceClockType ClockType { get; set; }

        // Problem detected with this event/segment
        public AttendanceExceptionType ExceptionType { get; set; }
    }
}
