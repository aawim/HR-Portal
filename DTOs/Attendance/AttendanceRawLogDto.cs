namespace HRM.DTOs.Attendance
{
    public class AttendanceRawLogDto
    {
        public int AttendanceLogId { get; set; }

        public int IndividualId { get; set; }

        public int InOutModeId { get; set; }

        public DateTime LogDateTime { get; set; }

        public string Mode =>
            InOutModeId == 1 ? "IN" :
            InOutModeId == 2 ? "OUT" :
            "UNKNOWN";
    }
}
