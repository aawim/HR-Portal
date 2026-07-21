using HRM.Enum;

namespace HRM.DTOs
{
    public class UserJobDto
    {
        public int JobId { get; set; }

        public int IndividualId { get; set; }

        //public int OrganisationId { get; set; }

        public int OrganisationBusinessEntityId { get; set; }

        public int OrganisationStructureId { get; set; }

        public int PositionId { get; set; }

        public string PositionName { get; set; } = string.Empty;

        public string DepartmentName { get; set; } = string.Empty;

        public EmploymentStatus Status { get; set; }

        public DateTime JoinedDate { get; set; }

        public DateTime? TerminatedDate { get; set; }

        public UserJobDto? ActiveJob { get; set; }

        public IReadOnlyList<UserJobDto> Jobs { get; set; } = new List<UserJobDto>();
    }
}
