using HRM.DTOs.UserContext;
namespace HRM.Application.Contracts.Pages;

/// <summary>Exposes only the current verified session; arbitrary user-context lookup and cache invalidation stay backend-only.</summary>
public interface ICurrentEmployeeSession
{
    Task<UserContextDto?> GetAsync(CancellationToken ct = default);
}
