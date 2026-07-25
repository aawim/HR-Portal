namespace HRM.DTOs.WorkPlanning
{
    public class GenerateWorkPlanResultDto
    {
        public bool Success { get; set; }

        public long? WorkPlanId { get; set; }

        public Guid? PlanGuid { get; set; }

        public int GeneratedSegmentCount { get; set; }

        public List<string> Errors { get; set; } = [];
    }
}
