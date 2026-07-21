using HRM.Components.Shared;
using HRM.DTOs.UserContext;
using HRM.Enum;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HRM.Services
{
    public class UserContextBuilder : IUserContextBuilder
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly IRoleService _roleService;
        //private readonly IJobService _jobService;
        private readonly IOrganisationService _organisationService;
        private IEffectivePermissionService _effectivePermissionService;

        public UserContextBuilder(
            IDbContextFactory<HrmTeContext> dbFactory,
            AuthenticationStateProvider authenticationStateProvider,
            IRoleService roleService,
            //IJobService jobService,
            IOrganisationService organisationService, IEffectivePermissionService effectivePermissionService)
        {
            _dbFactory = dbFactory;
            _authenticationStateProvider = authenticationStateProvider;
            _roleService = roleService;
            //_jobService = jobService;
            _organisationService = organisationService;
            _effectivePermissionService = effectivePermissionService;

            
        }

        public async Task<UserContextDto?> BuildAsync(int userId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            var user = await LoadUserByIdAsync(db, userId);

            if (user == null)
                return null;

            return await BuildContextAsync(db, user);
        }

        public async Task<UserContextDto?> BuildByUsernameAsync(string username)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            var user = await LoadUserByUsernameAsync(db, username);

            if (user == null)
                return null;

            return await BuildContextAsync(db, user);
        }

        public async Task<UserContextDto?> BuildByBusinessEntityAsync(int businessEntityId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            var user = await LoadUserByBusinessEntityAsync(db, businessEntityId);

            if (user == null)
                return null;

            return await BuildContextAsync(db, user);
        }

        public async Task<UserContextDto?> BuildCurrentUserAsync()
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();

            var username = authState.User.FindFirst("idnumber")?.Value;

            if (string.IsNullOrWhiteSpace(username))
                return null;

            return await BuildByUsernameAsync(username);
        }

     

        private Task<User?> LoadUserByIdAsync(HrmTeContext db, int userId)
               => LoadUserAsync(db, u => u.UserId == userId);

        private Task<User?> LoadUserByUsernameAsync(HrmTeContext db, string username)
            => LoadUserAsync(db, u => u.Username == username);

        private Task<User?> LoadUserByBusinessEntityAsync(HrmTeContext db, int businessEntityId)
            => LoadUserAsync(db, u => u.BusinessEntityID == businessEntityId);


        private async Task<User?> LoadUserAsync(HrmTeContext db,Expression<Func<User, bool>> predicate)
        {
            return await db.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(predicate);
        }



        private async Task<UserContextDto> BuildContextAsync(
         HrmTeContext db,
         User user)
        {
            var context = await BuildBasicContextAsync(db, user);


            await PopulateJobAsync(context);


            await PopulateOrganisationAsync(context);


            await PopulateRoleGroupsAsync(db, context);


            await PopulatePermissionsAsync(db, context);


            await PopulateSecurityAsync(context);


            await PopulateOrganisationAccessAsync(db, context);

            // Determine status
            context.Status = DetermineStatus(context);

            // Validate eligibility
            context.Validation = EvaluateUserState(context);


            return context;
        }

        private async Task PopulateOrganisationAccessAsync(
    HrmTeContext db,
    UserContextDto context)
        {
            context.OrganisationAccess =
                await db.UserOrganisations
                    .AsNoTracking()
                    .Where(x =>
                        x.Users.Any(u =>
                            u.UserId == context.UserId))
                    .Select(x => new UserOrganisationAccessDto
                    {
                        UserOrganisationID =
                            x.UserOrganisationID,

                        BusinessEntityID =
                            x.BusinessEntityID,

                        OrganisationName =
                            x.Organisation.OrganisationName
                    })
                    .ToListAsync();
        }


        private async Task PopulateSecurityAsync(
            UserContextDto context)
                {
                    context.UserGroups =
                        await _roleService
                        .GetUserRoleGroupsAsync(
                            context.UserId);


                    context.Permissions =
                        await _roleService.GetUserPermissionsAsync(context.UserId);
                }


        private UserValidationResultDto EvaluateUserState(UserContextDto context)
        {
            var result = new UserValidationResultDto();

            // 1. User exists
            if (context.UserId == 0)
            {
                result.Eligibility = UserEligibility.UserNotFound;

                result.Errors.Add(
                    "User record not found.");

                return result;
            }

            // 2. Account status
            if (context.Status == UserStatus.Suspended ||
                context.Status == UserStatus.Inactive)
            {
                result.Eligibility =
                    UserEligibility.UserInactive;

                result.Errors.Add(
                    "User account is not active.");

                return result;
            }

            // 3. Individual exists
            if (context.IndividualId == 0)
            {
                result.Eligibility =
                    UserEligibility.NoIndividual;

                result.Errors.Add(
                    "No individual profile linked.");

                return result;
            }

            // 4. Active job
            if (context.ActiveJob == null)
            {
                result.Eligibility =
                    UserEligibility.NoActiveJob;

                result.Errors.Add(
                    "No active job found.");

                return result;
            }

            // 5. Organisation
            if (context.Organisation == null)
            {
                result.Eligibility =
                    UserEligibility.NoOrganisation;

                result.Errors.Add(
                    "No organisation assigned.");

                return result;
            }

            // 6. Role Groups
            if (!context.UserGroups.Any())
            {
                result.Eligibility =
                    UserEligibility.NoRoleGroup;

                result.Errors.Add(
                    "No role group assigned.");

                return result;
            }

            // 7. Permissions
            if (!context.Permissions.Any())
            {
                result.Eligibility =
                    UserEligibility.NoPermission;

                result.Errors.Add(
                    "No permissions assigned.");

                return result;
            }

            // Everything is valid
            result.Eligibility =
                UserEligibility.Eligible;

            return result;
        }

        private UserStatus DetermineStatus(
    UserContextDto context)
        {
            if (context.UserId == 0)
                return UserStatus.Unknown;

            if (context.ActiveJob == null)
                return UserStatus.Terminated;

            return UserStatus.Active;
        }




        private async Task<UserContextDto> BuildBasicContextAsync(
       HrmTeContext db,
       User user)
        {
            var individual = await db.Individuals
                .AsNoTracking()
                .SingleOrDefaultAsync(x =>
                    x.BusinessEntityId == user.BusinessEntityID);


            return new UserContextDto
            {
                UserId = user.UserId,

                BusinessEntityId = user.BusinessEntityID,

                IndividualId = individual?.BusinessEntityId ?? 0,

                Username = user.Username,

                FullName = individual == null
                    ? user.Username
                    : $"{individual.FirstNameEnglish} {individual.MiddleNameEnglish} {individual.LastNameEnglish}"
                        .Replace("  ", " ")
                        .Trim()
            };
        }

        private async Task PopulateJobAsync(UserContextDto context)
        {
            if (context.IndividualId == 0)
            {
                context.ActiveJob = null;
                return;
            }

            context.ActiveJob = await GetActiveJobAsync(context.IndividualId);
        }




        private async Task<ActiveJobDto?> GetActiveJobAsync(int individualId)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync();

            return await db.Jobs
                .AsNoTracking()
                .Where(x =>
                    x.IndividualID == individualId &&
                    x.JobStateId == SharedConfig.JobStates.APPROVED &&
                    x.TerminatedDate == null)
                .Select(x => new ActiveJobDto
                {
                    JobID = x.JobId,

                    IndividualID = x.IndividualID,

                    OrganisationID = x.OrganisationID,

                    OrganisationStructureID =
                        x.OrganisationStructureId,

                    JobStateID =
                        x.JobStateId,

                    JobTypeID =
                        x.JobTypeId,

                    JoinedDate =
                        x.JoinedDate,

                    SAPNumber =
                        x.Sapnumber,

                    IsActive = true
                })
                .FirstOrDefaultAsync();
        }







        private async Task PopulateOrganisationAsync(
          UserContextDto context)
        {
            if (context.BusinessEntityId == 0)
            {
                context.Organisation = null;
                return;
            }

            context.Organisation = await _organisationService.GetOrganisationAsync(context.BusinessEntityId);
        }


        private async Task<List<UserGroupDto>> LoadUserGroupsAsync(HrmTeContext db, int userId)
        {
            return await db.UserAssignedUserGroups
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => new UserGroupDto
                {
                    UserGroupID = x.UserGroupId,

                    GroupName =
                        x.UserGroup.GroupName,

                    UserOrganisationID =
                        x.UserGroup.UserOrganisationId,

                    ParentUserGroupID =
                        x.UserGroup.ParentUserGroupId,

                    IsGlobal =
                        x.UserGroup.IsGlobal
                })
                .ToListAsync();
        }



        private async Task PopulateRoleGroupsAsync(
            HrmTeContext db,
            UserContextDto context)
                {
                    context.UserGroups = await LoadUserGroupsAsync(db,context.UserId);
        }


        private async Task PopulatePermissionsAsync(HrmTeContext db,UserContextDto context)
        {
            //var permissions = new List<UserPermissionDto>();

            //// 1. Permissions from Role Groups
            //var groupPermissions = await LoadGroupPermissionsAsync(db,context.UserGroups);

            //permissions.AddRange(groupPermissions);


            //// 2. Direct user permissions
            //var directPermissions = await LoadDirectPermissionsAsync(db,context.UserId);

            //permissions.AddRange(directPermissions);

            //context.Permissions =
            //    permissions
            //        .GroupBy(x => x.RoleID)
            //        .Select(x => x.First())
            //        .ToList();

            context.PermissionSources = await _effectivePermissionService.GetPermissionSourcesAsync(context.UserId);


            context.Permissions =await _effectivePermissionService.GetEffectivePermissionsAsync(context.UserId);
        }


        private async Task<List<UserPermissionDto>> LoadGroupPermissionsAsync(
    HrmTeContext db,
    List<UserGroupDto> roleGroups)
        {
            var groupIds = roleGroups
                .Select(x => x.UserGroupID)
                .ToList();


            return await db.UserGroupRoles
                .AsNoTracking()
                .Where(x =>
                    groupIds.Contains(x.UserGroupId) &&
                    x.IsActive)
                .Select(x => new UserPermissionDto
                {
                    RoleID = x.RoleId,

                    RoleKey = x.Role.RoleKey,

                    Name = x.Role.Name,

                    Description = x.Role.Description,

                    ModuleID = x.Role.ModuleId,

                    IsSystemRole = x.Role.IsSystemRole == true
                })
                .ToListAsync();
        }


        private async Task<List<UserPermissionDto>> LoadDirectPermissionsAsync(
       HrmTeContext db,
       int userId)
        {
            return await db.UserRoles
                .AsNoTracking()
                .Where(x =>
                    x.UserId == userId &&
                    x.IsActive)
                .Select(x => new UserPermissionDto
                {
                    RoleID = x.RoleId,

                    RoleKey = x.Role.RoleKey,

                    Name = x.Role.Name,

                    Description = x.Role.Description,

                    ModuleID = x.Role.ModuleId,

                    IsSystemRole = x.Role.IsSystemRole == true
                })
                .ToListAsync();
        }










        //private async Task PopulatePermissionsAsync(
        //HrmTeContext db,
        //UserContextDto context)
        //{
        //    if (!context.RoleGroups.Any())
        //    {
        //        context.Permissions = new();

        //        return;
        //    }


        //    var userGroupIds = context.RoleGroups
        //        .Select(x => x.UserGroupID)
        //        .ToList();


        //    var permissions = await db.UserGroupRoles
        //        .AsNoTracking()
        //        .Where(x =>
        //            userGroupIds.Contains(x.UserGroupId) &&
        //            x.IsActive)
        //        .Select(x => new UserPermissionDto
        //        {
        //            RoleID = x.RoleId,

        //            RoleKey = x.Role.RoleKey,

        //            Name = x.Role.Name,

        //            Description = x.Role.Description,

        //            ModuleID = x.Role.ModuleId,

        //            IsSystemRole =
        //                x.Role.IsSystemRole == true,

        //            UserGroupID =
        //                x.UserGroupId,

        //            UserGroupName =
        //                x.UserGroup.GroupName
        //        })
        //        .ToListAsync();


        //    context.Permissions = permissions
        //        .GroupBy(x => x.RoleID)
        //        .Select(x => x.First())
        //        .ToList();
        //}


        //private async Task<List<UserPermissionDto>> LoadDirectOrganisationPermissionsAsync(
        //HrmTeContext db,
        //int userId)
        //    {
        //        return await db.UserRoles
        //            .AsNoTracking()
        //            .Where(x =>
        //                x.UserId == userId &&
        //                x.IsActive &&
        //                x.UserOrganisationId != null)
        //            .Select(x => new UserPermissionDto
        //            {
        //                RoleID = x.RoleId,

        //                RoleKey = x.Role.RoleKey,

        //                Name = x.Role.Name,

        //                Description = x.Role.Description,

        //                ModuleID = x.Role.ModuleId,

        //                IsSystemRole = x.Role.IsSystemRole == true
        //            })
        //            .ToListAsync();
        //}








        // To be implemented

        //    return await db.Users
        //.AsNoTracking()
        //.Where(u => u.Username == username)
        //.OrderByDescending(u => u.UserID)
        //.FirstOrDefaultAsync();



    }
}
