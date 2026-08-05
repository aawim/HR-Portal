namespace HRM.DTOs.Profile
{
    public class JobDetailDto
    {
        public int JobId { get; set; }

        public int IndividualId { get; set; }

        public int OrganisationId { get; set; }

        public string OrganisationName { get; set; } = string.Empty;

        public string DepartmentName { get; set; } = string.Empty;

        public int? OrganisationStructureId { get; set; }

        public string OrganisationStructureName { get; set; } = string.Empty;

        public int? PositionId { get; set; }

        public string PositionName { get; set; } = string.Empty;

        public int? JobTypeId { get; set; }

        public string JobTypeName { get; set; } = string.Empty;

        public string EmployeeNumber { get; set; } = string.Empty;

        public DateTime JoinedDate { get; set; }

        public DateTime? ConfirmedDate { get; set; }

        public DateTime? ProbationEndDate { get; set; }

        public DateTime? TerminatedDate { get; set; }

        public bool IsActive { get; set; }

        public string EmploymentStatus { get; set; } = string.Empty;

        public int? SupervisorIndividualId { get; set; }

        public string SupervisorName { get; set; } = string.Empty;

        public int ServiceYears { get; set; }

        public int ServiceMonths { get; set; }

        public string ServiceDuration { get; set; } = string.Empty;
    }
}
