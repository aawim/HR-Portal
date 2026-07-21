namespace HRM.Models.WorkPlanning
{
    public partial class WorkAssignmentSegment
    {
        public long WorkAssignmentSegmentId { get; set; }

        public long WorkAssignmentId { get; set; }

        public int WorkTemplateSegmentId { get; set; }

        public int WorkSegmentTypeId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int SequenceNumber { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime? EndDateTime { get; set; }

        public int GraceBeforeMinutes { get; set; }

        public int GraceAfterMinutes { get; set; }

        public bool IsMandatory { get; set; }

        public bool RequiresAttendance { get; set; }

        public bool RequiresLocationValidation { get; set; }

        public bool RequiresDeviceValidation { get; set; }

        public bool IsValid { get; set; }

        public int? OperationLogId { get; set; }

        public virtual WorkAssignment WorkAssignment { get; set; } = null!;

        public virtual WorkTemplateSegment WorkTemplateSegment { get; set; } = null!;

        public virtual WorkSegmentType WorkSegmentType { get; set; } = null!;

        public virtual ICollection<AttendanceLogResolution> AttendanceLogResolutions { get; set; }
            = new List<AttendanceLogResolution>();
    }
}
