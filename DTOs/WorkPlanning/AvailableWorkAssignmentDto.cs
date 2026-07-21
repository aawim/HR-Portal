namespace HRM.DTOs.WorkPlanning
{
    public sealed class AvailableWorkAssignmentDto
    {
        public long WorkAssignmentId { get; set; }

        public string DisplayName { get; set; } =
            string.Empty;
    }
}
