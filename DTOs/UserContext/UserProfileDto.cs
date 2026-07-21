namespace HRM.DTOs.UserContext
{
    public class UserProfileDto
    {
        public int UserId { get; set; }

        public int IndividualID { get; set; }

        public string Username { get; set; } = "";

        public string Email { get; set; } = "";

        public string Mobile { get; set; } = "";

        public string FullName { get; set; } = "";

        public string? OrganisationName { get; set; }

        public JobDto? ActiveJob { get; set; }

        public List<JobDto> JobHistory { get; set; } = new();

        public List<UserRoleDto> Roles { get; set; } = new();
    }
}
