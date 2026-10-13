namespace HRM.DTOs.ApprovalFlow
{
    public class WorkflowStartContextDto
    {
        public int RequestId { get; set; }

        public int RequestTypeId { get; set; }

        public int OrganisationBusinessEntityId { get; set; }

        public int SubjectJobId { get; set; }

        public int JobTypeId { get; set; }

        public int StartedByUserId { get; set; }
    }
}
