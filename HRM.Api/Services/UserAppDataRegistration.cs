using HRM.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.Services;

public static class UserAppDataRegistration
{
    public static IServiceCollection AddUserAppData(this IServiceCollection services)
    {
        services.AddScoped<UserAppService>();
        services.AddScoped<IUserAppDataService>(provider => provider.GetRequiredService<UserAppService>());
        return services;
    }
}
