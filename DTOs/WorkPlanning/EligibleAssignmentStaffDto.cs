namespace HRM.DTOs.WorkPlanning
{
    public sealed class EligibleAssignmentStaffDto
    {
        public int IndividualId { get; set; }

        public int JobId { get; set; }

        public string EmployeeName { get; set; } =
            string.Empty;

        public string JobTitle { get; set; } =
            string.Empty;

        public bool IsSelected { get; set; }
    }
}
