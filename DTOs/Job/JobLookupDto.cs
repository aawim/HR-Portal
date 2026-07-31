namespace HRM.DTOs.Job
{
    public class JobLookupDto
    {
        public int JobId { get; set; }

        public int IndividualId { get; set; }

        public string EmployeeName { get; set; } = string.Empty;

        public string PositionName { get; set; } = string.Empty;

        public string DisplayName =>
            $"{EmployeeName} - {PositionName}";
    }
}
