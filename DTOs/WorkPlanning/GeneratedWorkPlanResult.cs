namespace HRM.DTOs.WorkPlanning
{
    public class GeneratedWorkPlanResult
    {

        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public long WorkPlanId { get; set; }

        public long? WorkAssignmentId { get; set; }
 

        public List<long> WorkAssignmentIds { get; set; } = [];
    

        public string? ErrorMessage { get; set; }
 

        //public bool SkipExistingAssignments { get; set; }

        //public bool ForceRegeneration { get; set; }

        //public bool ValidateConflicts { get; set; }

        //public bool PublishImmediately { get; set; }

        //public int? RequestId { get; set; }

  

        public string? TemplateName { get; set; }

        public string? AssignmentTitle { get; set; }

        public DateTime? StartDateTime { get; set; }

        public DateTime? EndDateTime { get; set; }

        public int SegmentCount { get; set; }

        public int OwnerCount { get; set; }
    }
}
