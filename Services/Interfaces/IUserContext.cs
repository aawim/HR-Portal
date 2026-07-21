using HRM.DTOs;

namespace HRM.Services.Interfaces
{
    public interface IUserContext
    {
        Task<int> GetUserIdAsync();
        Task<int> GetIndividualIdAsync();
        Task<int> GetJobIdAsync();
        Task<string?> GetUsernameAsync();
        Task<UserSessionDto?> GetSessionAsync();
    }
}
