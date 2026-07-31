namespace HRM.DTOs.WorkPlanning
{
    public sealed class ManualAssignmentJobDto
    {
        public int JobId { get; set; }

        public int IndividualId { get; set; }

        public int OrganisationId { get; set; }

        public string EmployeeName { get; set; } =
            string.Empty;

        public string PositionName { get; set; } =
            string.Empty;

        public string? EmployeeNumber { get; set; }

        public string? OrganisationName { get; set; } 
    }


   

}
