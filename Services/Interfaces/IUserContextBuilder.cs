using HRM.DTOs.UserContext;

namespace HRM.Services.Interfaces
{
    public interface IUserContextBuilder
    {
        // Main Builders
        Task<UserContextDto?> BuildAsync(int userId);
        Task<UserContextDto?> BuildByUsernameAsync(string username);
        Task<UserContextDto?> BuildByBusinessEntityAsync(int businessEntityId);
        Task<UserContextDto?> BuildCurrentUserAsync();
    }
}
