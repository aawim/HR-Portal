using HRM.DTOs.UserContext;

namespace HRM.Services.Interfaces
{
    public interface IUserContextCache
    {
        Task<UserContextDto?> GetAsync(int userId);

        Task SetAsync(
            int userId,
            UserContextDto context);

        Task RemoveAsync(int userId);

        Task ClearAsync();
    }
}
