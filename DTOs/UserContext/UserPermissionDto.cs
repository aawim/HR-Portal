using HRM.Enum;

namespace HRM.DTOs.UserContext
{
    public class UserPermissionDto
    {
    
        // Permission
        public int RoleID { get; set; }

        public string RoleKey { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }


        // Module
        public int? ModuleID { get; set; }


        // Role Information
        public bool IsSystemRole { get; set; }


        // Source Information

        public PermissionSource Source { get; set; }


        // Direct User Permission
        public int? UserID { get; set; }


        // User Group Permission

        public int? UserGroupID { get; set; }

        public string? UserGroupName { get; set; }


        // Organisation Permission

        public int? OrganisationID { get; set; }


        // Scope

        public bool IsGlobal { get; set; }
    }
}
