using HRM.Enum;
namespace HRM.DTOs.Attendance
{
    public class AttendanceExceptionDto
    {
        public DateTime Date { get; set; }

        public AttendanceExceptionType ExceptionType { get; set; }

        public string? Description { get; set; }

        public AttendanceLogDto? CheckIn { get; set; }

        public AttendanceLogDto? CheckOut { get; set; }

        public int? MinutesLate { get; set; }

        public bool IsExcused { get; set; }
    }
}
