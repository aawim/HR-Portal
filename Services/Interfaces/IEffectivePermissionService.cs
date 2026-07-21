using HRM.DTOs.UserContext;

namespace HRM.Services.Interfaces
{
    public interface IEffectivePermissionService
    {
        Task<List<UserPermissionDto>>GetEffectivePermissionsAsync(int userId);

        Task<bool>HasPermissionAsync(int userId,string permissionKey);

        Task<List<UserPermissionDto>> LoadDirectPermissionsAsync(int userId);

        Task<List<UserPermissionDto>>GetPermissionSourcesAsync(int userId);


    }
}
