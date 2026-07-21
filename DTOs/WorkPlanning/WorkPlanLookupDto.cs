namespace HRM.DTOs.WorkPlanning
{
    public sealed class WorkPlanLookupDto
    {
        public int WorkPlanId { get; set; }
        public string DisplayName { get; set; } =
        string.Empty;
    }
}
