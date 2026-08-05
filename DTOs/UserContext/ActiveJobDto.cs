using HRM.DTOs.Team;

namespace HRM.DTOs.UserContext
{
    public class ActiveJobDto
    {

        // Identity
        public int JobId { get; set; }

        public int IndividualId { get; set; }

        public int OrganisationId { get; set; }

        // Organisation
        public string OrganisationName { get; set; } =
            string.Empty;

        public int? OrganisationStructureId { get; set; }

        public string OrganisationStructureName { get; set; } =
            string.Empty;

        public string DepartmentName { get; set; } = string.Empty;
         

        public List<ActiveJobTeamDto> Teams { get; set; } = [];



        // Job state
        public int JobStateId { get; set; }

        public string JobStateName { get; set; } =
            string.Empty;

        // Job type
        public int? JobTypeId { get; set; }

        public string JobTypeName { get; set; } =
            string.Empty;

        // Position
        public int? PositionId { get; set; }

        public string PositionName { get; set; } = string.Empty;

        // Employment information
        public string EmployeeNumber { get; set; } =
            string.Empty;

        public string EmploymentType { get; set; } =
            string.Empty;

        public DateTime JoinedDate { get; set; }

        public DateTime? ConfirmedDate { get; set; }

        public DateTime? TerminatedDate { get; set; }

        public bool IsActive { get; set; }

        // Supervisor
        public int? SupervisorIndividualId { get; set; }

        public string SupervisorName { get; set; } =
            string.Empty;

        // Payroll / external references
        public decimal? BasicSalary { get; set; }

        public string? SAPNumber { get; set; }

        // Calculated service duration
        public int ServiceYears { get; set; }

        public int ServiceMonths { get; set; }

        public string ServiceDurationText { get; set; } =
            string.Empty;











        //   public int JobID { get; set; }

        //   public int IndividualID { get; set; }

        //   public int OrganisationID { get; set; }

        //   public string? OrganisationName { get; set; }

        //   public int? OrganisationStructureID { get; set; }

        //   public string? OrganisationStructureName { get; set; }


        //   public int JobStateID { get; set; }

        //   public string? JobStateName { get; set; }


        //   public int? JobTypeID { get; set; }

        //   public string? JobTypeName { get; set; }


        //   public DateTime JoinedDate { get; set; }

        //   public DateTime? TerminatedDate { get; set; }

        //   public decimal? BasicSalary { get; set; }

        //   public string? SAPNumber { get; set; }

        //   public bool IsActive { get; set; }


        //   public int JobId { get; set; }

        //   public int IndividualId { get; set; }

        //   public int OrganisationId { get; set; }

        //  public int? OrganisationStructureId { get; set; }

        // public int? JobTypeId { get; set; }

        //public int? PositionId { get; set; }

        //   public string PositionName { get; set; } =
        //       string.Empty;

        //   public string EmployeeNumber { get; set; } =
        //       string.Empty;



        //   public int ServiceYears { get; set; }

        //   public int ServiceMonths { get; set; }

        //   public string ServiceDurationText { get; set; } =
        //       string.Empty;
    }
}
