using HRM.Enum;

namespace HRM.DTOs.WorkPlanning
{
    public sealed class AssignWorkAssignmentRequest
    {
        public int WorkTemplateId { get; set; }

        public int OrganisationBusinessEntityId { get; set; }


        public int WorkAssignmentId { get; set; }

 
        public int IndividualId { get; set; }

        public int OperationLogId { get; set; }
        

        public int JobId { get; set; }

        public DateTime WorkDate { get; set; }


        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }

        public TimeOnly ScheduledStartTime { get; set; }

        public int? PlanningProviderId { get; set; }

        public WorkPlanGenerationSource GenerationSource { get; set; }

        public int GeneratedByUserId { get; set; }

        //public long OperationLogId { get; set; }

        public string? AssignmentTitle { get; set; }

        public string? AssignmentDescription { get; set; }

        public string? Remarks { get; set; }

        public bool RequiresAttendance { get; set; }

        public bool RequiresCheckout { get; set; }

        public int Priority { get; set; }

        public string? AssignmentSource { get; set; }
    }
}
