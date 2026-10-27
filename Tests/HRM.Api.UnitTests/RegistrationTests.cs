using System.Reflection;
using HRM.Application.Contracts;
using HRM.Api.Infrastructure;
using HRM.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
namespace HRM.Api.UnitTests;

public sealed class RegistrationTests
{
    [Fact]
    public void All_reviewed_contracts_and_controllers_resolve_without_background_workers_or_production_configuration()
    {
        var factory = new QueryFactory();
        using var server = new TestServer(new WebHostBuilder().UseEnvironment("Testing")
            .UseDefaultServiceProvider(options => { options.ValidateOnBuild = true; options.ValidateScopes = true; })
            .ConfigureServices(services =>
            {
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
                services.AddScoped(_ => factory.CreateDbContext());
                services.AddSingleton<IDbContextFactory<HrmTeContext>>(factory);
                services.AddDbContext<AuditDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
                services.AddHrmBackendServices(includeBackgroundWorkers: false);
            }).Configure(_ => { }));
        using var scope = server.Services.CreateScope();
        foreach (var contract in ApplicationContractCatalog.All) Assert.NotNull(scope.ServiceProvider.GetRequiredService(contract));
        Assert.DoesNotContain(server.Services.GetServices<IHostedService>(), service => service is HRM.Services.Leave.LeaveAccrualWorker);
        var actions = server.Services.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items.OfType<ControllerActionDescriptor>()
            .Where(a => a.ControllerTypeInfo.IsSubclassOf(typeof(HrmApiController)) && a.AttributeRouteInfo?.Template?.StartsWith("api/v1/", StringComparison.Ordinal) == true).ToArray();
        Assert.Equal(331, actions.Length); // 329 supported operations + identity and CSRF token endpoints. Two obsolete aliases are excluded.
        foreach (var action in actions)
        {
            Assert.NotNull(ActivatorUtilities.CreateInstance(scope.ServiceProvider, action.ControllerTypeInfo.AsType()));
            Assert.True(action.ControllerTypeInfo.IsDefined(typeof(AutoValidateAntiforgeryTokenAttribute), true));
            Assert.True(action.ControllerTypeInfo.IsDefined(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true));
        }
        var routes = actions.Select(a => (a.AttributeRouteInfo!.Template, string.Join(",", a.ActionConstraints!.OfType<Microsoft.AspNetCore.Mvc.ActionConstraints.HttpMethodActionConstraint>().SelectMany(c => c.HttpMethods)))).ToArray();
        Assert.Equal(routes.Length, routes.Distinct().Count());
        Assert.Contains(actions, action => action.AttributeRouteInfo!.Template == "api/v1/individual-registry/search");
        Assert.Contains(actions, action => action.AttributeRouteInfo!.Template == "api/v1/individual/get-individual-id");
    }
}
