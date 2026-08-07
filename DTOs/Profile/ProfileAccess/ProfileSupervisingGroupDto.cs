namespace HRM.DTOs.Profile.ProfileAccess
{
    public class ProfileSupervisingGroupDto
    {
        public int TeamId { get; set; }

        public string TeamName { get; set; } = string.Empty;

        public int OrganisationId { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; }
    }
}
