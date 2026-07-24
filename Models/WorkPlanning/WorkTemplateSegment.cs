namespace HRM.Models.WorkPlanning
{
    public class WorkTemplateSegment
    {
        public int WorkTemplateSegmentId { get; set; }

        public int WorkTemplateId { get; set; }

        public int WorkSegmentTypeId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int SequenceNumber { get; set; }

        public int OffsetMinutes { get; set; }

        public int? DurationMinutes { get; set; }

        public int GraceBeforeMinutes { get; set; }

        public int GraceAfterMinutes { get; set; }

        public bool IsMandatory { get; set; }

        public bool RequiresAttendance { get; set; }

        public bool RequiresLocationValidation { get; set; }

        public bool RequiresDeviceValidation { get; set; }

        public bool IsActive { get; set; }

        public bool IsPaid { get; set; }

        public int OperationLogId { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public virtual WorkTemplate WorkTemplate { get; set; } = null!;

        public virtual WorkSegmentType WorkSegmentType { get; set; } = null!;

        public virtual ICollection<WorkAssignmentSegment>
            WorkAssignmentSegments
        { get; set; }
                = new List<WorkAssignmentSegment>();
    }
}
