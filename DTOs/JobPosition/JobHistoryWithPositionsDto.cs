namespace HRM.DTOs.JobPosition
{
    public class JobHistoryWithPositionsDto
    {
        public int JobId { get; set; }

        public int IndividualId { get; set; }

        public int OrganisationId { get; set; }

        public string OrganisationName { get; set; } =
            string.Empty;

        public int? OrganisationStructureId { get; set; }

        public string OrganisationStructureName { get; set; } =
            string.Empty;

        public int JobStateId { get; set; }

        public string JobStateName { get; set; } =
            string.Empty;

        public int JobTypeId { get; set; }

        public string JobTypeName { get; set; } =
            string.Empty;

        public DateTime JoinedDate { get; set; }

        public DateTime? TerminatedDate { get; set; }

        public bool IsCurrentJob { get; set; }

        public string EmploymentPeriodText { get; set; } =
            string.Empty;

        public List<JobPositionHistoryDto> Positions { get; set; } =
            [];
    }
}
