namespace HRM.DTOs.WorkPlanning
{
    public sealed class WorkAssignmentPreviewDto
    {
        public int WorkTemplateId { get; set; }

        public string TemplateName { get; set; } =
            string.Empty;

        public DateTime AssignmentBaseDateTime { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

        public List<WorkAssignmentSegmentPreviewDto> Segments
        {
            get;
            set;
        } = [];
    }

    public sealed class WorkAssignmentSegmentPreviewDto
    {
        public int WorkTemplateSegmentId { get; set; }

        public string Name { get; set; } =
            string.Empty;

        public int SequenceNumber { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

        public bool IsMandatory { get; set; }

        public bool RequiresAttendance { get; set; }

        public bool RequiresLocationValidation { get; set; }

        public bool RequiresDeviceValidation { get; set; }
    }
}
