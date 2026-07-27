using HRM.Enum;

namespace HRM.DTOs.WorkPlanning
{

  
    
    public class WorkPlanListDto
    {

        public string EmployeeName { get; set; } = string.Empty;
        public int? OrganisationId { get; set; }
        public string OrganisationName { get; set; } = string.Empty;
        public string PlanningProviderName { get; set; } = string.Empty;


        public long WorkPlanId { get; set; }

        public int IndividualId { get; set; }

        public int JobId { get; set; }

        public int OrganisationBusinessEntityId { get; set; }

        public int PlanningProviderId { get; set; }

        public int? WorkTemplateId { get; set; }

        public DateTime WorkDate { get; set; }

        public WorkPlanGenerationSource GenerationSource { get; set; }

        public DateTime GeneratedDate { get; set; }

        public bool IsGenerated { get; set; }

        public bool IsManual { get; set; }

        public bool IsValid { get; set; }

        public bool IsFinalized { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public int Version { get; set; }

        public string IndividualName { get; set; } = string.Empty;

        public string WorkTemplateName { get; set; } = string.Empty;
  
        public DateTime? FinalizedDate { get; set; }

        public Guid PlanGuid { get; set; }

 
        public List<WorkPlanSegmentDto> Segments { get; set; } = [];

    }
}
