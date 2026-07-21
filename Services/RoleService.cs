using HRM.DTOs.Roles;
using HRM.DTOs.UserContext;
using HRM.Enum;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class RoleService : IRoleService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private IEffectivePermissionService _effectivePermissionService;

        public RoleService(IDbContextFactory<HrmTeContext> dbFactory, IEffectivePermissionService effectivePermissionService)
        {
            _dbFactory = dbFactory;
            _effectivePermissionService = effectivePermissionService;
        }

        public async Task<List<UserPermissionDto>> GetDirectPermissionsAsync(int userId)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync();


            return await (
                from userRole in db.UserRoles

                join role in db.Roles
                    on userRole.RoleId equals role.RoleID


                where userRole.UserId == userId
                      &&
                      userRole.IsActive


                select new UserPermissionDto
                {
                    RoleID = role.RoleID,

                    RoleKey = role.RoleKey,

                    Name = role.Name,

                    Description = role.Description,

                    ModuleID = role.ModuleId,

                    IsSystemRole =
                        role.IsSystemRole == true,

                    Source =
                        PermissionSource.DirectUser
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<UserPermissionDto>> GetGroupPermissionsAsync(List<int> userGroupIds)
            {
                await using var db =
                    await _dbFactory.CreateDbContextAsync();


                var result =
                    await (
                        from userGroupRole in db.UserGroupRoles

                        join role in db.Roles
                            on userGroupRole.RoleId equals role.RoleID


                        where userGroupIds.Contains(
                                userGroupRole.UserGroupId)
                              &&
                              userGroupRole.IsActive


                        select new UserPermissionDto
                        {
                            RoleID = role.RoleID,

                            RoleKey = role.RoleKey,

                            Name = role.Name,

                            Description = role.Description,

                            ModuleID = role.ModuleId,

                            IsSystemRole =
                                role.IsSystemRole == true,

                            Source =
                                PermissionSource.UserGroup
                        })
                        .AsNoTracking()
                        .ToListAsync();


                return result;
        }

        public async Task<List<UserPermissionDto>> GetUserPermissionsAsync(int userId)
        {
            if (userId <= 0)
                return new List<UserPermissionDto>();

            return await _effectivePermissionService.GetEffectivePermissionsAsync(userId);
        }



        public async Task<List<UserGroupDto>> GetUserRoleGroupsAsync(int userId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();


            var result = await (
            from assigned in db.UserAssignedUserGroups

            join groups in db.UserGroups
                on assigned.UserGroupId equals groups.UserGroupId

            where assigned.UserId == userId

            select new UserGroupDto
            {
                UserGroupID = groups.UserGroupId,

                GroupName = groups.GroupName,

                UserOrganisationID = groups.UserOrganisationId,

                ParentUserGroupID = groups.ParentUserGroupId,

                IsGlobal = groups.IsGlobal
            })
            .AsNoTracking()
            .ToListAsync();


            return result;

        }






    }
}