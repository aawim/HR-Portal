using HRM.DTOs.UserContext;

namespace HRM.Services.Interfaces
{
    public interface IUserPermissionService
    {
        Task<List<UserPermissionDto>>GetDirectPermissionsAsync(int userId);
         

    }
}
