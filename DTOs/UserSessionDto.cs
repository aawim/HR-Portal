namespace HRM.DTOs
{
    public class UserSessionDto
    {
        public int UserId { get; set; }
        public int IndividualId { get; set; }
        public int JobId { get; set; }
        public int? OrganisationId { get; set; }
        public string OrganisationName { get; set; } = "";
        public bool CanAccessAdminPortal { get; set; }
    }
}
