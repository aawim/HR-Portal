using HRM.Application.Contracts;
using HRM.Application.Contracts.Pages;
using HRM.Application.Transport;
using HRM.Services.Interfaces;
using HRM.UI.Authentication;

namespace HRM.UI.Services.Transport;

public static class ApiClientRegistration
{
    public static IServiceCollection AddHrmApiClients(this IServiceCollection services)
    {
        services.AddScoped<IApiOperationClient>(provider =>
        {
            var redirecting = 0;
            return new HttpApiOperationClient(new HttpClient(
                new CircuitBackendHandler(provider.GetRequiredService<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>(),
                    provider.GetRequiredService<BackendSessionStore>())) { BaseAddress = provider.GetRequiredService<BackendOrigins>().Api },
                () =>
                {
                    if (Interlocked.Exchange(ref redirecting, 1) == 0)
                        provider.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()
                            .NavigateTo("/Online/Auth/login", forceLoad: true);
                });
        });
        var create = typeof(ApiContractProxy).GetMethod(nameof(ApiContractProxy.Create))!;
        foreach (var contract in ApplicationContractCatalog.All)
            services.AddScoped(contract, provider => create.MakeGenericMethod(contract).Invoke(null, [provider.GetRequiredService<IApiOperationClient>()])!);
        services.AddScoped<IUserContextService, EmployeeContextClient>();
        services.AddScoped<IUserService, UserProfileClient>();
        services.AddScoped<IGeographyService, GeographyClient>();
        services.AddScoped<HRM.Services.ToastService>();
        services.AddScoped<HRM.Services.State.LeaveApplicationState>();
        services.AddScoped<HRM.Services.Stores.LeaveStore>();
        services.AddScoped<HRM.Helpers.NavigationTrail>(p => new(p.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().Uri));
        services.AddScoped<HRM.Services.PortalContext>();
        return services;
    }
}

public sealed class EmployeeContextClient(ICurrentEmployeeSession session) : IUserContextService
{
    public HRM.DTOs.UserContext.UserContextDto? Current { get; private set; }
    public async Task<HRM.DTOs.UserContext.UserContextDto?> GetCurrentAsync() => Current = await session.GetAsync();
    public async Task<HRM.DTOs.UserContext.UserContextDto?> GetAsync(int userId)
    {
        var current = await GetCurrentAsync();
        if (current is not null && current.UserId != userId) throw new UnauthorizedAccessException();
        return current;
    }
    public Task<HRM.DTOs.UserContext.UserContextDto?> RefreshAsync(int userId) => GetAsync(userId);
    public Task InvalidateAsync(int userId) { if (Current?.UserId == userId) Current = null; return Task.CompletedTask; }
}

public sealed class UserProfileClient(HRM.Services.Contracts.UserServiceContracts.IOperations users) : IUserService
{
    public Task<HRM.DTOs.UserContext.UserProfileDto?> GetMyProfileAsync() => users.GetMyProfileAsync();
    public Task<HRM.DTOs.UserContext.UserProfileDto?> GetByIdAsync(int userId) => users.GetByIdAsync(userId);
    public Task<HRM.DTOs.UserContext.UserProfileDto?> GetByUsernameAsync(string username) => users.GetByUsernameAsync(username);
    public Task<HRM.DTOs.UserContext.UserProfileDto?> GetByBusinessEntityAsync(int id) => users.GetByBusinessEntityAsync(id);
    public Task<bool> UpdateUserAsync(HRM.DTOs.UserContext.UserProfileDto profile) => users.UpdateUserAsync(profile);
}
