using HRM.DTOs.UserContext;
using HRM.Enum;
using HRM.Services.Interfaces;

namespace HRM.Services
{
    public class UserAccessService : IUserAccessService
    {
        private readonly IUserContextService _userContextService;


        public UserAccessService(
            IUserContextService userContextService)
        {
            _userContextService = userContextService;
        }

 


        public async Task<int> GetCurrentJobIdAsync()
        {
            var context = await RequireContextAsync();

            if (context.ActiveJob == null)
                throw new InvalidOperationException(
                    "Current user has no Active Job.");

            return context.ActiveJob.JobID;
        }

        public async Task<int> GetCurrentOrganisationIdAsync()
        {
            var context = await RequireContextAsync();

            if (context.Organisation == null)
                throw new InvalidOperationException(
                    "Current user has no organisation.");

            return context.Organisation.OrganisationBusinessEntityId;
        }


        public async Task<int> GetCurrentIndividualIdAsync()
        {
            var context = await RequireContextAsync();

            if (context.IndividualId == 0)
                throw new InvalidOperationException(
                    "Current user is not linked to an Individual.");

            return context.IndividualId;
        }

        public async Task<UserContextDto?> GetContextAsync()
        {
            return await _userContextService.GetCurrentAsync();
        }

        public async Task<UserContextDto> RequireContextAsync()
        {
            var context =
                await _userContextService
                    .GetCurrentAsync();


            if (context == null)
            {
                throw new UnauthorizedAccessException(
                    "User context is not available.");
            }


            return context;
        }


        public async Task<bool> HasPermissionAsync(string permissionKey)
        {
            var context = await GetContextAsync();


            if (context == null)
                return false;


            return context.Permissions.Any(x =>
                x.RoleKey.Equals(
                    permissionKey,
                    StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> HasUserGroupAsync(string userGroupName)
        {
            var context = await GetContextAsync();


            if (context == null)
                return false;


            return context.UserGroups.Any(x =>
                x.GroupName.Equals(
                    userGroupName,
                    StringComparison.OrdinalIgnoreCase));
        }

        public async Task<UserValidationResultDto> ValidateUserAsync()
        {
            var context =
                await GetContextAsync();


            if (context == null)
            {
                return new UserValidationResultDto
                {
                    Eligibility =
                        UserEligibility.UserNotFound
                };
            }


            return context.Validation;
        }
        public async Task<bool> HasAnyUserGroupAsync(params string[] groups)
        {
            var context =
                await GetContextAsync();


            if (context == null)
                return false;


            return context.UserGroups.Any(x =>
                groups.Contains(
                    x.GroupName,
                    StringComparer.OrdinalIgnoreCase));
        }

        public async Task<bool> HasAllUserGroupsAsync(params string[] groups)
        {
            var context =
                await GetContextAsync();


            if (context == null)
                return false;


            return groups.All(group =>
                context.UserGroups.Any(x =>
                    x.GroupName.Equals(
                        group,
                        StringComparison.OrdinalIgnoreCase)));
        }

        public async Task<bool> HasOrganisationAccessAsync(int organisationId)
        {
            var context =
                await GetContextAsync();


            if (context == null)
                return false;


            return context.OrganisationAccess.Any(x =>
                x.BusinessEntityID == organisationId);
        }




        public async Task<bool> IsEligibleAsync()
        {
            var context =
                await GetContextAsync();

            return context?
                .Validation
                .Eligibility
                == UserEligibility.Eligible;
        }


        public async Task<bool> HasActiveJobAsync()
        {
            var context =
                await GetContextAsync();

            return context?.ActiveJob != null;
        }
    }
}
