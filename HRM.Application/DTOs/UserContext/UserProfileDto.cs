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

        public string? ImageUrl { get; set; }

        public string? OrganisationName { get; set; }

        public JobDto? ActiveJob { get; set; }

        public List<JobDto> JobHistory { get; set; } = new();

        public List<UserRoleDto> Roles { get; set; } = new();

        public string? FullNameDhivehi { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Nationality { get; set; }
        public string? NationalId { get; set; }
        public string? PassportNumber { get; set; }
        public string? CurrentPositionName { get; set; }
        public string? CurrentAddress { get; set; }
        public string? PermanentAddress { get; set; }
        public int ActiveDocumentsCount { get; set; }
        public int TotalLeavesTakenDays { get; set; }
        public List<UserProfileContactDto> Contacts { get; set; } = new();
        public List<UserProfileLeaveDto> RecentLeaves { get; set; } = new();
    }

    public sealed record UserProfileContactDto(string Type, string Value);
    public sealed record UserProfileLeaveDto(int LeaveId, string LeaveType, DateTime FromDate, DateTime ToDate, int Duration, string Status);
}
