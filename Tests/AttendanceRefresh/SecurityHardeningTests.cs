using HRM.DTOs.UserContext;
using HRM.Helpers;
using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

static class SecurityHardeningTests
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static async Task Denied(Func<Task> action)
    {
        try { await action(); }
        catch (UnauthorizedAccessException) { return; }
        throw new Exception("Expected access denial before a database write.");
    }

    public static async Task RunAsync()
    {
        var assembly = typeof(AttendanceLogService).Assembly;
        using (var settingsFile = System.Text.Json.JsonDocument.Parse(File.ReadAllText("appsettings.json")))
        {
            var trackedSettings = settingsFile.RootElement;
            Check(string.IsNullOrWhiteSpace(trackedSettings.GetProperty("Authentication").GetProperty("OpenIdConnect").GetProperty("ClientSecret").GetString()),
                "Tracked settings contain a login secret.");
            foreach (var connection in trackedSettings.GetProperty("ConnectionStrings").EnumerateObject())
                Check(string.IsNullOrWhiteSpace(connection.Value.GetString()), "Tracked settings contain an environment-specific database connection.");
        }
        var routes = assembly.GetTypes().SelectMany(t => t.GetCustomAttributes(typeof(Microsoft.AspNetCore.Components.RouteAttribute), true)
            .Cast<Microsoft.AspNetCore.Components.RouteAttribute>()).Select(r => r.Template).ToList();
        Check(!routes.Contains("/debug/attendance-resolver"), "Public attendance debug route survived cleanup.");
        Check(!routes.Contains("/admin/work-planning/assignment-resolver"), "Unrestricted resolver route survived cleanup.");
        Check(assembly.GetTypes().All(t => t.Name != "LeaveTypesController"), "Duplicate unsafe leave CRUD API survived cleanup.");
        Check(typeof(HRM.Controllers.OrganisationController).IsDefined(typeof(AuthorizeAttribute), true), "Organisation API permits anonymous access.");
        Check(typeof(HRM.Controllers.LogoutController).GetMethod("Logout")!.IsDefined(typeof(ValidateAntiForgeryTokenAttribute), true), "Logout does not validate CSRF tokens.");
        Check(DatabaseConnectionPolicy.Validate("Server=localhost;Database=test;Integrated Security=True;Encrypt=True;TrustServerCertificate=False", false, "test").Length > 0,
            "Valid encrypted deployment configuration was rejected.");
        foreach (var connection in new[] { "", "Server=localhost;Encrypt=False", "Server=localhost;Encrypt=True;TrustServerCertificate=True" })
        {
            try { DatabaseConnectionPolicy.Validate(connection, false, "test"); throw new Exception("Unsafe production database configuration was accepted."); }
            catch (InvalidOperationException) { }
        }
        await using (var configured = new HrmTeContext(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase("provider-security-check").Options))
            Check(configured.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory", "DbContext replaced its configured provider.");
        await using (var unconfigured = new HrmTeContext())
        {
            try { _ = unconfigured.Database.ProviderName; throw new Exception("DbContext inferred an unconfigured database."); }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Configure HrmTeContext")) { }
        }

        var actor = new UserContextDto { UserId = 1, IndividualId = 100 };
        var developer = false;
        var access = Proxy.For<IUserAccessService>((method, args) => method switch
        {
            "RequireContextAsync" or "GetContextAsync" => Task.FromResult(actor),
            "GetCurrentOrganisationIdAsync" => Task.FromResult(10),
            "HasOrganisationAccessAsync" => Task.FromResult((int)args[0]! == 10),
            "HasPermissionAsync" => Task.FromResult(false),
            "HasUserGroupAsync" => Task.FromResult(developer && (string)args[0]! == "Developer"),
            _ => throw new Exception(method)
        });
        var clock = new AttendanceLogService(null!, null!, null!, null!, access);
        await Denied(() => clock.AddClockEventAsync(200));
        await Denied(() => clock.AddClockEventAsync(100, DateTime.UtcNow.AddHours(5).AddDays(-1)));
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Security:AllowTestClock"] = "true" }).Build();
        var environment = new SecurityTestEnvironment { EnvironmentName = Environments.Development };
        clock = new AttendanceLogService(null!, null!, null!, null!, access, environment: environment, configuration: settings);
        Check(!await clock.CanUseTestClockAsync(), "A non-developer can use the test clock.");
        developer = true;
        Check(await clock.CanUseTestClockAsync(), "Explicit development diagnostic access was not honoured.");
        environment.EnvironmentName = Environments.Production;
        Check(!await clock.CanUseTestClockAsync(), "Production permits custom test clocks.");
        var types = new LeaveTypeCatalogueService(null!, access);
        await Denied(() => types.CreateAsync(new HRM.DTOs.LeaveTypes.LeaveTypeCatalogueSaveRequest { Name = "Attempt" }));
        await Denied(() => types.UpdateAsync(new HRM.DTOs.LeaveTypes.LeaveTypeCatalogueSaveRequest { LeaveTypeId = 1, Name = "Attempt" }));

        await using var db = new TestContext(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(), o => o.EnableNullChecks(false)).Options);
        db.Jobs.AddRange(new Job { JobId = 1, IndividualID = 100, OrganisationID = 10 },
            new Job { JobId = 2, IndividualID = 200, OrganisationID = 20 },
            new Job { JobId = 3, IndividualID = 200, OrganisationID = 10 });
        await db.SaveChangesAsync();
        await StaffDataAccess.RequireJobAsync(db, access, 1);
        await Denied(() => StaffDataAccess.RequireJobAsync(db, access, 2));
        await Denied(() => StaffDataAccess.RequireJobAsync(db, access, 3));
        actor.IsHRStaff = true;
        await StaffDataAccess.RequireJobAsync(db, access, 3);
        await Denied(() => StaffDataAccess.RequireJobAsync(db, access, 2));
        await Denied(() => LeaveApplicantScope.ResolveAsync(db, access, 2));
    }
}

sealed class SecurityTestEnvironment : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "HRM.Tests";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "HRM.UI", "wwwroot");
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}

sealed class RenderServices(IServiceProvider original) : IServiceProvider
{
    public object? GetService(Type type) => original.GetService(type) ??
        (type == typeof(Microsoft.AspNetCore.Hosting.IWebHostEnvironment) ? new SecurityTestEnvironment() :
         type == typeof(Microsoft.JSInterop.IJSRuntime) ? new NoBrowserInterop() : null);
}

sealed class NoBrowserInterop : Microsoft.JSInterop.IJSRuntime
{
    public ValueTask<T> InvokeAsync<T>(string identifier, object?[]? args) => throw new InvalidOperationException("Browser actions require an explicit test stub.");
    public ValueTask<T> InvokeAsync<T>(string identifier, CancellationToken token, object?[]? args) => InvokeAsync<T>(identifier, args);
}
