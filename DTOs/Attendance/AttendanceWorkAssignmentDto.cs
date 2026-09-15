using HRM.Enum;

namespace HRM.DTOs.Attendance
{
    public class AttendanceWorkAssignmentDto
    {
        public long WorkAssignmentId { get; set; }

        public int WorkTemplateTypeId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Code { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

        public WorkAssignmentSource AssignmentSource { get; set; }

        public WorkOwnershipType OwnershipType { get; set; }

        public bool RequiresAttendance { get; set; }

        public bool RequiresCheckOut { get; set; }

        public int Priority { get; set; }

        public bool IsValid { get; set; }
    }
}
