using HRM.Enum;

namespace HRM.DTOs.WorkPlanning
{
    public class GenerateWorkPlanRequest
    {
        public int WorkTemplateId { get; set; }

        public int OrganisationBusinessEntityId { get; set; }

        public int IndividualId { get; set; }

        public int JobId { get; set; }

        public DateOnly WorkDate { get; set; }

        public int? PlanningProviderId { get; set; }

        public WorkPlanGenerationSource GenerationSource { get; set; }

        public WorkAssignmentSource AssignmentSource { get; set; }

        public int? GeneratedByUserId { get; set; }

        public int OperationLogId { get; set; }

        public string? Remarks { get; set; }

        public string? AssignmentTitle { get; set; }

        public string? AssignmentDescription { get; set; }

        public bool IsMandatory { get; set; } = true;

        public bool RequiresAttendance { get; set; } = true;

        public bool RequiresCheckout { get; set; } = true;

        public int Priority { get; set; } = 1;

    }
}
