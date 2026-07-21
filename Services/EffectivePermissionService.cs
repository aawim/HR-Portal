using HRM.DTOs.UserContext;
using HRM.Enum;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class EffectivePermissionService : IEffectivePermissionService
    {

        private readonly IDbContextFactory<HrmTeContext> _dbFactory;


        public EffectivePermissionService(IDbContextFactory<HrmTeContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<UserPermissionDto>> GetEffectivePermissionsAsync(int userId)
        {
            var permissions = new List<UserPermissionDto>();

            permissions.AddRange(
                await LoadDirectPermissionsAsync(userId));

            permissions.AddRange(
                await LoadUserGroupPermissionsAsync(userId));

            permissions.AddRange(
                await LoadOrganisationPermissionsAsync(userId));

            return ResolveEffectivePermissions(permissions);
        }

        public async Task<bool> HasPermissionAsync(int userId,string permissionKey)
        {
            var permissions = await GetEffectivePermissionsAsync(userId);

            return permissions.Any(x =>
                x.RoleKey.Equals(
                    permissionKey,
                    StringComparison.OrdinalIgnoreCase));
        }

       private List<UserPermissionDto>ResolveEffectivePermissions(List<UserPermissionDto> permissions)
        {
            return permissions
                .GroupBy(x => x.RoleID)
                .Select(group =>
                {
                    // Priority:
                    // 1. Direct User
                    // 2. Organisation
                    // 3. UserGroup

                    return group
                        .OrderBy(x =>
                            GetPermissionPriority(x.Source))
                        .First();

                })
                .ToList();
        }

        private int GetPermissionPriority(PermissionSource source)
        {
            return source switch
            {
                PermissionSource.DirectUser => 1,

                PermissionSource.Organisation => 2,

                PermissionSource.UserGroup => 3,

                _ => 99
            };
        }

        public async Task<List<UserPermissionDto>>GetPermissionSourcesAsync(int userId)
        {
            var permissions =new List<UserPermissionDto>();


            var directPermissions = await LoadDirectPermissionsAsync(userId);

            permissions.AddRange(directPermissions);



            var groupPermissions = await LoadUserGroupPermissionsAsync(userId);

            permissions.AddRange(groupPermissions);



            var organisationPermissions = await LoadOrganisationPermissionsAsync(userId);

            permissions.AddRange(organisationPermissions);



            return permissions;
        }

        public async Task<List<UserPermissionDto>>LoadDirectPermissionsAsync(int userId)
        {

            using var db = await _dbFactory.CreateDbContextAsync();


            return await db.UserRoles
                .AsNoTracking()
                .Where(x =>
                    x.UserId == userId &&
                    x.IsActive)
                .Select(x => new UserPermissionDto
                {
                    RoleID = x.RoleId,

                    RoleKey =
                        x.Role.RoleKey,

                    Name =
                        x.Role.Name,

                    Description =
                        x.Role.Description,

                    ModuleID =
                        x.Role.ModuleId,

                    IsSystemRole =
                        x.Role.IsSystemRole == true,

                    Source =
                        PermissionSource.DirectUser,

                    UserID =
                        x.UserId,

                    UserGroupID = null,

                    UserGroupName = null,

                    OrganisationID =
                        x.UserOrganisationId,

                    IsGlobal = 
                        x.UserOrganisationId == null
                })
                .ToListAsync();
        }

        public async Task<List<UserPermissionDto>>LoadUserGroupPermissionsAsync(int userId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            return await db.UserAssignedUserGroups
                .AsNoTracking()
                .Where(x =>
                    x.UserId == userId)
                .SelectMany(x =>
                    x.UserGroup.UserGroupRoles
                    .Where(r =>
                        r.IsActive)
                    .Select(r => new UserPermissionDto
                    {
                        RoleID =
                            r.RoleId,

                        RoleKey =
                            r.Role.RoleKey,

                        Name =
                            r.Role.Name,

                        Description =
                            r.Role.Description,

                        ModuleID =
                            r.Role.ModuleId,

                        IsSystemRole =
                            r.Role.IsSystemRole == true,


                        Source =
                            PermissionSource.UserGroup,


                        UserID =
                            userId,


                        UserGroupID =
                            x.UserGroupId,


                        UserGroupName =
                            x.UserGroup.GroupName,


                        OrganisationID =
                            x.UserGroup.UserOrganisationId,


                        IsGlobal =
                            x.UserGroup.IsGlobal
                    }))
                .ToListAsync();
        }

        private async Task<List<UserPermissionDto>>LoadOrganisationPermissionsAsync(int userId)
        {

            using var db = await _dbFactory.CreateDbContextAsync();
            return await db.UserOrganisationRoles
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.UserOrganisation.Users
                        .Any(u =>
                            u.UserId == userId))
                .Select(x => new UserPermissionDto
                {
                    RoleID =
                        x.RoleID,

                    RoleKey =
                        x.Role.RoleKey,

                    Name =
                        x.Role.Name,

                    Description =
                        x.Role.Description,

                    ModuleID =
                        x.Role.ModuleId,

                    IsSystemRole =
                        x.Role.IsSystemRole == true,


                    Source =
                        PermissionSource.Organisation,


                    UserID =
                        userId,


                    UserGroupID = null,

                    UserGroupName = null,


                    OrganisationID =
                        x.UserOrganisation.BusinessEntityID,


                    IsGlobal = false
                })
                .ToListAsync();
        }

        
    }
}
