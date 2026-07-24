namespace HRM.DTOs.WorkPlanning
{
    public class WorkTemplateSegmentDto
    {
        public int WorkTemplateSegmentId { get; set; }

        public int WorkTemplateId { get; set; }

        public int WorkSegmentTypeId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public TimeOnly? StartTime { get; set; }

        public TimeOnly? EndTime { get; set; }

        public DateOnly? CreatedAt { get; set; }

        public DateOnly? UpdatedAt { get; set; }

        public bool IsPaid { get; set; }

        public bool RequiresAttendance { get; set; }

        public bool RequiresLocationValidation { get; set; }

        public bool RequiresDeviceValidation { get; set; }

        public int DisplayOrder { get; set; }

        public int OperationLogId { get; set; }

        public bool IsActive { get; set; }

  

        public string WorkSegmentTypeName { get; set; } =
            string.Empty;

    

        public int SequenceNumber { get; set; }

        public int OffsetMinutes { get; set; }

        public int DurationMinutes { get; set; }

        public int GraceBeforeMinutes { get; set; }

        public int GraceAfterMinutes { get; set; }

        public bool IsMandatory { get; set; }

 
    }
}
