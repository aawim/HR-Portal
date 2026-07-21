using HRM.DTOs.UserContext;

namespace HRM.Services.Interfaces
{
    public interface IUserContextService
    {
        Task<UserContextDto?> GetCurrentAsync();
        Task<UserContextDto?> GetAsync(int userId);
        Task<UserContextDto?> RefreshAsync(int userId);

 

        //Task RefreshAsync();
        Task InvalidateAsync(int userId);
        UserContextDto? Current { get; }
    }
}
