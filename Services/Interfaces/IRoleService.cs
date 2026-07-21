using HRM.DTOs.UserContext;

namespace HRM.Services.Interfaces
{
    public interface IRoleService
    {
        Task<List<UserGroupDto>> GetUserRoleGroupsAsync(int userId);
        Task<List<UserPermissionDto>> GetUserPermissionsAsync(int userId);
        Task<List<UserPermissionDto>> GetGroupPermissionsAsync(List<int> userGroupIds);
        Task<List<UserPermissionDto>> GetDirectPermissionsAsync(int userId);
    }
}
