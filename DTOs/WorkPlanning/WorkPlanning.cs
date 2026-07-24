namespace HRM.DTOs.WorkPlanning
{
    public class WorkPlanning
    {
        public int WorkTemplateId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public TimeSpan? DefaultStartTime { get; set; }

        public TimeSpan? DefaultEndTime { get; set; }

        public string TemplateType { get; set; } = "RegularShift";

        public bool RequiresAttendance { get; set; }

        public bool RequiresCheckOut { get; set; }

        public bool RequiresLocationValidation { get; set; }

        public bool RequiresDeviceValidation { get; set; }

        public bool AllowsOverlap { get; set; }

        public bool IsValid { get; set; }

        public int SegmentCount { get; set; }
    }
}
