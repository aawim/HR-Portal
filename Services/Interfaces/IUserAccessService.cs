using HRM.DTOs.UserContext;
namespace HRM.Services.Interfaces
{
    public interface IUserAccessService
    {

        // Current user context
        Task<UserContextDto?> GetContextAsync();

        Task<UserContextDto> RequireContextAsync();

        // Permissions
        Task<bool> HasPermissionAsync(string permissionKey);

        // User Groups
        Task<bool> HasUserGroupAsync(string userGroupName);

        Task<bool> HasAnyUserGroupAsync(params string[] groups);

        Task<bool> HasAllUserGroupsAsync(params string[] groups);

        // Organisation Scope
        Task<bool> HasOrganisationAccessAsync(int organisationId);

        // User State
        Task<bool> IsEligibleAsync();

        Task<bool> HasActiveJobAsync();

        // Detailed validation
        Task<UserValidationResultDto> ValidateUserAsync();

        Task<int> GetCurrentOrganisationIdAsync();
        Task<int> GetCurrentJobIdAsync();
        Task<int> GetCurrentIndividualIdAsync();


        


    }
}

