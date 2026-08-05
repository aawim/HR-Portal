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
 
 

  
        public int JobID { get; set; }
        public string? TemplateName { get; set; }

        public string? AssignmentTitle { get; set; }

        public DateTime? StartDateTime { get; set; }

        public DateTime? EndDateTime { get; set; }

        public int SegmentCount { get; set; }

        public int OwnerCount { get; set; }

        public int Version {  get; set; }


        public static GeneratedWorkPlanResult Failure(string errorMessage)
        {
            return new GeneratedWorkPlanResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        public static GeneratedWorkPlanResult Successful(
            int workPlanId,
            int workAssignmentId)
        {
            return new GeneratedWorkPlanResult
            {
                Success = true,
                WorkPlanId = workPlanId,
                WorkAssignmentId = workAssignmentId
            };
        }
    }
}
