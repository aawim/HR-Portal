namespace HRM.DTOs.WorkPlanning
{
    public class WorkTemplateSegmentGenerationDto
    {
        public int WorkTemplateSegmentId { get; set; }

        public int WorkTemplateId { get; set; }

        public int WorkSegmentTypeId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int SequenceNumber { get; set; }

        public int OffsetMinutes { get; set; }

        public int DurationMinutes { get; set; }

        public int GraceBeforeMinutes { get; set; }

        public int GraceAfterMinutes { get; set; }

        public bool IsMandatory { get; set; }

        public bool RequiresAttendance { get; set; }

        public bool RequiresLocationValidation { get; set; }

        public bool RequiresDeviceValidation { get; set; }
    }
}
