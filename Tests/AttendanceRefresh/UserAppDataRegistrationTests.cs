using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.Services.Repositories;
using HRM.Services.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

static class UserAppDataRegistrationTests
{
    public static async Task RunAsync()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDbContextFactory<HrmTeContext>>(new NoDatabaseFactory());
        services.AddScoped<UserRepository>();
        services.AddScoped<IUserContext>(_ => Proxy.For<IUserContext>((method, _) => throw new Exception("Unexpected identity call: " + method)));
        services.AddScoped<IUserAccessService>(_ => Proxy.For<IUserAccessService>((method, _) => method == "RequireContextAsync"
            ? Task.FromResult(new UserContextDto { UserId = 1, IndividualId = 1 }) : throw new Exception(method)));
        services.AddUserAppData();
        services.AddScoped<LeaveStore>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<LeaveStore>();
        var service = scope.ServiceProvider.GetRequiredService<IUserAppDataService>();
        if (!ReferenceEquals(service, scope.ServiceProvider.GetRequiredService<UserAppService>()))
            throw new Exception("Interface and implementation must share their scoped instance.");
        await store.LoadAsync();
        if (store.JobLeaveTypes.Count != 0) throw new Exception("A user without an active job must have no leave assignments.");
        using var nextScope = provider.CreateScope();
        if (ReferenceEquals(service, nextScope.ServiceProvider.GetRequiredService<IUserAppDataService>()))
            throw new Exception("User application data must not leak across scopes.");
        Console.WriteLine("PASS LeaveStore startup: interface registration, validated dependencies, scoped identity and no-active-job loading");
    }

    sealed class NoDatabaseFactory : IDbContextFactory<HrmTeContext>
    {
        public HrmTeContext CreateDbContext() => throw new Exception("This startup test must not access a database.");
    }
}
