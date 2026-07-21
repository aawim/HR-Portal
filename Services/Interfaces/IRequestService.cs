using HRM.DTOs;
using HRM.Models;

namespace HRM.Services.Interfaces
{
    public interface IRequestService
    {
        Task<Request> CreateAsync(CreateRequestDto dto);

        Task<Request?> GetByIdAsync(int requestId);

        Task<bool> ChangeStateAsync(
            int requestId,
            int requestStateId,
            string? remarks = null);

        //Task<bool> DeleteAsync(int requestId);
    }
}
