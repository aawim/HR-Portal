using HRM.Application.Contracts.Pages;
using HRM.DTOs.UserContext;
using HRM.Services.Interfaces;
namespace HRM.Services.Queries;
public sealed class CurrentEmployeeSession(IUserAccessService access) : ICurrentEmployeeSession
{
    public Task<UserContextDto?> GetAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return access.GetContextAsync();
    }
}
