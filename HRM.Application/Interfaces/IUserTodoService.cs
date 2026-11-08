using HRM.DTOs;

namespace HRM.Services.Interfaces;

public interface IUserTodoService
{
    Task<UserTodoPage> GetMyTodosAsync(bool includeCompleted, int page);
    Task SaveAsync(UserTodoDto model);
    Task SetCompletedAsync(int id, byte[] rowVersion, bool completed);
    Task DeleteAsync(int id, byte[] rowVersion);
}
