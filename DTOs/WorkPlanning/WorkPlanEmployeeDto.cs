namespace HRM.DTOs.WorkPlanning
{
    public class WorkPlanEmployeeDto
    {
        public int IndividualId { get; set; }

        public int JobId { get; set; }

        public int OrganisationId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? EmployeeNumber { get; set; }

        public string? PositionName { get; set; }

        public string? OrganisationName { get; set; }

        public string Description =>
            string.Join(
                " | ",
                new[]
                {
                EmployeeNumber,
                PositionName,
                OrganisationName
                }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}
