using HRM.Enum;

namespace HRM.DTOs.UserContext
{
    public class UserContextDto
    {
        // ==========================
        // Identity
        // ==========================

        public int UserId { get; set; }

        public int BusinessEntityId { get; set; }

        public int IndividualId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;


        // ==========================
        // Status
        // ==========================

        public UserStatus Status { get; set; } = UserStatus.Unknown;



        // ==========================
        // Employment
        // ==========================

        public ActiveJobDto? ActiveJob { get; set; }



        // ==========================
        // Current Organisation
        // ==========================

        public UserOrganisationDto? Organisation { get; set; }




        // ==========================
        // Security
        // ==========================

        public List<UserGroupDto> UserGroups { get; set; } = new();

        public List<UserPermissionDto> PermissionSources { get; set; } = new();
        public List<UserPermissionDto> Permissions { get; set; } = new();



        // ==========================
        // Organisation scope
        // ==========================
        public List<UserOrganisationAccessDto> OrganisationAccess { get; set; } = new();



        // ==========================
        // Validation
        // ==========================

        public UserValidationResultDto Validation { get; set; } = new();


        public bool IsSuperAdministrator { get; set; }

        public bool IsHRStaff { get; set; }

        public bool IsHeadOfOrganisation { get; set; }

        public int? UserOrganisationId { get; set; }

        public bool IsHRManager { get; set; }

        public bool IsHRReceptionist { get; set; }

        public bool IsOrganisationAdmin { get; set; }

        public bool IsSupervisor { get; set; }

        public bool IsHRHead { get; set; }
   

        public bool HasPermission(string permission)
        {
            return Permissions.Any(x =>
                x.RoleKey.Equals(
                    permission,
                    StringComparison.OrdinalIgnoreCase));
        }


        public bool HasModuleAccess(int moduleId)
        {
            return Permissions.Any(x =>
                x.ModuleID == moduleId);
        }

        public bool HasPermission(
        string permissionKey,
        int organisationId)
            {
                return Permissions.Any(x =>
                    x.RoleKey.Equals(
                        permissionKey,
                        StringComparison.OrdinalIgnoreCase)
                    &&
                    (
                        x.OrganisationID == null ||
                        x.OrganisationID == organisationId
                    ));
            }

        public bool HasUserGroup(string groupName)
        {
            return UserGroups.Any(x =>
                x.GroupName.Equals(
                    groupName,
                    StringComparison.OrdinalIgnoreCase));
        }



    }
 


}
