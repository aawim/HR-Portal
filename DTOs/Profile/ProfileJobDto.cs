namespace HRM.DTOs.Profile
{
    public class ProfileJobDto
    {

        public int JobId { get; set; }

        public string? EmployeeNumber { get; set; }

        public int OrganisationId { get; set; }

        public string? OrganisationName { get; set; }

        public int? OrganisationStructureId { get; set; }

        public string? OrganisationStructureName { get; set; }

        public string? DesignationName { get; set; }

        public DateTime? JoinedDate { get; set; }

        public DateTime? TerminatedDate { get; set; }

        public bool IsActive { get; set; }

    }
}
