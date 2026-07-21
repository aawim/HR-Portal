namespace HRM.DTOs.UserContext
{
    public class ActiveJobDto
    {
        public int JobID { get; set; }

        public int IndividualID { get; set; }

        public int OrganisationID { get; set; }

        public string? OrganisationName { get; set; }

        public int? OrganisationStructureID { get; set; }

        public string? OrganisationStructureName { get; set; }


        public int JobStateID { get; set; }

        public string? JobStateName { get; set; }


        public int? JobTypeID { get; set; }

        public string? JobTypeName { get; set; }


        public DateTime JoinedDate { get; set; }

        public DateTime? TerminatedDate { get; set; }

        public decimal? BasicSalary { get; set; }

        public string? SAPNumber { get; set; }

        public bool IsActive { get; set; }
    }
}
