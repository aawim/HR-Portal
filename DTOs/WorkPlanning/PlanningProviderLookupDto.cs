namespace HRM.DTOs.WorkPlanning
{
    public class PlanningProviderLookupDto
    {
        public int PlanningProviderId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
