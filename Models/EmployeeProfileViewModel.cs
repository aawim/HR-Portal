namespace HRM.Models
{
    public class EmployeeProfileViewModel
    {
        // Personal Info
        public string FullName { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public string SAPNumber { get; set; } = string.Empty;
        public string NidPassport { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Island { get; set; } = string.Empty;
        public string Atoll { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;

        // Contact Info
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        // Emergency Contact
        public string EmergencyName { get; set; } = string.Empty;
        public string EmergencyRelation { get; set; } = string.Empty;
        public string EmergencyEmail { get; set; } = string.Empty;
        public string EmergencyMobile { get; set; } = string.Empty;

        // Job Info
        public string Organisation { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public string EmployeeType { get; set; } = string.Empty;
        public string JobState { get; set; } = string.Empty;
        public DateTime ActiveDate { get; set; }
        public decimal BasicSalary { get; set; }
    }
}
