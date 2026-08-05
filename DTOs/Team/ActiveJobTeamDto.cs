namespace HRM.DTOs.Team
{
    public class ActiveJobTeamDto
    {
        public int TeamId { get; set; }

        public string Name { get; set; } =
            string.Empty;

        public string? NameDhivehi { get; set; }

        public bool IsSupervisor { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
