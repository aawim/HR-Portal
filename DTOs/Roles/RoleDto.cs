namespace HRM.DTOs.Roles
{
    public class RoleDto
    {
        public string Name { get; set; } = string.Empty;
        public string RoleKey { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ContextID { get; set; } = 0;
    }
}
