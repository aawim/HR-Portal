namespace HRM.DTOs.Attendance
{
    public sealed class AttendanceWorkPlanDto
    {
        public long WorkPlanId { get; set; }

        public int IndividualId { get; set; }

        public int JobId { get; set; }

        public int OrganisationId { get; set; }

        public DateTime WorkDate { get; set; }

        public int? WorkTemplateId { get; set; }

        public bool IsFinalized { get; set; }

        public bool IsGenerated { get; set; }

        public bool IsManual { get; set; }

        public List<AttendanceWorkSegmentDto> Segments { get; set; } = [];
        public List<AttendanceWorkAssignmentDto> Assignments { get; set; } = [];

       
    }
}
