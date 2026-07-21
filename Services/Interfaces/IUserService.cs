using HRM.DTOs.UserContext;
using HRM.Models;

namespace HRM.Services.Interfaces
{
    public interface IUserService
    {
        Task<UserProfileDto?> GetByIdAsync(int userId);
        Task<UserProfileDto?> GetByUsernameAsync(string username);
        Task<UserProfileDto?> GetByBusinessEntityAsync(int businessEntityId);
        Task<bool> UpdateUserAsync(UserProfileDto profile);
    }
}
