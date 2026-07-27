namespace HRM.DTOs.WorkPlanning
{
    public class WorkPlanDetailDto
    {
        public long WorkPlanId { get; set; }

        public int IndividualId { get; set; }

        public int JobId { get; set; }

        public DateTime WorkDate { get; set; }

        public int? WorkTemplateId { get; set; }

        public string WorkTemplateName { get; set; } = "";

        public string? Remarks { get; set; }

        public bool IsGenerated { get; set; }

        public bool IsManual { get; set; }

        public bool IsFinalized { get; set; }

        public List<WorkPlanSegmentDto> Segments { get; set; } = [];
    }
}
