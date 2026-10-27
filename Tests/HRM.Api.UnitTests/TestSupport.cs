using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;
[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace HRM.Api.UnitTests;

public sealed class FakeAccess : IUserAccessService
{
    public UserContextDto? Actor { get; set; } = new() { UserId = 1, IndividualId = 100, BusinessEntityId = 100, Username = "alice" };
    public int Organisation { get; set; } = 10;
    public HashSet<string> Permissions { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Task<UserContextDto?> GetContextAsync() => Task.FromResult(Actor);
    public Task<UserContextDto> RequireContextAsync() => Task.FromResult(Actor ?? throw new UnauthorizedAccessException());
    public Task<bool> HasPermissionAsync(string permissionKey) => Task.FromResult(Permissions.Contains(permissionKey));
    public Task<bool> HasUserGroupAsync(string userGroupName) => Task.FromResult(false);
    public Task<bool> HasAnyUserGroupAsync(params string[] groups) => Task.FromResult(false);
    public Task<bool> HasAllUserGroupsAsync(params string[] groups) => Task.FromResult(false);
    public Task<bool> HasOrganisationAccessAsync(int organisationId) => Task.FromResult(Actor is not null && organisationId == Organisation);
    public Task<bool> IsEligibleAsync() => Task.FromResult(Actor is not null);
    public Task<bool> HasActiveJobAsync() => Task.FromResult(Actor?.ActiveJob is not null);
    public Task<UserValidationResultDto> ValidateUserAsync() => Task.FromResult(new UserValidationResultDto());
    public Task<int> GetCurrentOrganisationIdAsync() => Task.FromResult(Organisation);
    public Task<int> GetCurrentJobIdAsync() => Task.FromResult(Actor?.ActiveJob?.JobId ?? 0);
    public Task<int> GetCurrentIndividualIdAsync() => Task.FromResult(Actor?.IndividualId ?? 0);
}
public sealed class QueryFactory(bool ignoreTransactions = false) : IDbContextFactory<HrmTeContext>
{
    private readonly DbContextOptions<HrmTeContext> options = new DbContextOptionsBuilder<HrmTeContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false))
        .ConfigureWarnings(w => { if (ignoreTransactions) w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning); }).Options;
    public HrmTeContext CreateDbContext() => new QueryContext(options);
    private sealed class QueryContext(DbContextOptions<HrmTeContext> options) : HrmTeContext(options)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }
    }
}

public class StubProxy : System.Reflection.DispatchProxy
{
    private Func<string, object?[], object?> handler = null!;
    public static T For<T>(Func<string, object?[], object?> handler) where T : class
    {
        var value = Create<T, StubProxy>(); ((StubProxy)(object)value).handler = handler; return value;
    }
    protected override object? Invoke(System.Reflection.MethodInfo? method, object?[]? args) => handler(method!.Name, args ?? []);
}
