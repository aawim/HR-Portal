namespace HRM.DTOs.ApprovalFlow
{
    public class WorkflowRequestContextDto
    {
        public int RequestId { get; set; }

        public int RequestTypeId { get; set; }
        public int? FallbackApprovalRequestTypeId { get; set; }

        public int OrganisationBusinessEntityId { get; set; }

        public int ApplicantBusinessEntityId { get; set; }

        public int RequestStateId { get; set; }

        public int? SubjectJobId { get; set; }

        public int? JobTypeId { get; set; }

        public DateOnly EffectiveDate { get; set; }
    }
}
