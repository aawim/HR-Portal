using HRM.DTOs.UserContext;

namespace HRM.DTOs.Profile
{
    public class ProfileActiveJobDto
    {
        public int JobId { get; set; }

        public int OrganisationId { get; set; }

        public string OrganisationName { get; set; } = "";

        public int? OrganisationStructureId { get; set; }

        public string DepartmentName { get; set; } = "";

        public int? PositionId { get; set; }

        public string PositionName { get; set; } = "";

        public string EmploymentType { get; set; } = "";

        public string EmployeeNumber { get; set; } = "";

        public string SupervisorName { get; set; } = "";

        public DateTime JoinedDate { get; set; }

        public DateTime? ConfirmedDate { get; set; }

        public DateTime? TerminatedDate { get; set; }

        public bool IsActive { get; set; }

        public int YearsOfService { get; set; }

        public int MonthsOfService { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public static ProfileActiveJobDto Failure(string errorMessage)
        {
            return new ProfileActiveJobDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        public static ProfileActiveJobDto Successful(int JobId)
        {
            return new ProfileActiveJobDto
            {
                Success = true,
                JobId = JobId,
               
            };
        }




        public int IndividualID { get; set; }

        public int OrganisationID { get; set; }

     

        public int? OrganisationStructureID { get; set; }

        public string? OrganisationStructureName { get; set; }


        public int JobStateID { get; set; }

        public string? JobStateName { get; set; }


        public int? JobTypeID { get; set; }

        public string? JobTypeName { get; set; }

 

        public decimal? BasicSalary { get; set; }

        public string? SAPNumber { get; set; }

   
        public int IndividualId { get; set; }

   

        public int? JobTypeId { get; set; }

       



        public int ServiceYears { get; set; }

        public int ServiceMonths { get; set; }

        public string ServiceDurationText { get; set; } =
            string.Empty;


       


    }

 }
