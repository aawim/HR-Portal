using System.Reflection;
using HRM.Application.Presentation;
using HRM.Components.Shared;
using HRM.Services;
using HRM.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
namespace HRM.UI.UnitTests;

public sealed class OvertimePortalTests
{
    [Theory]
    [InlineData("Overtime Claim Request")]
    [InlineData("Overtime Preapproval Request")]
    [InlineData("Overtime Clock Event Request")]
    public void Action_notifications_route_to_admin_even_when_legacy_type_urls_point_to_staff(string type)
    {
        Assert.Equal("/admin/attendance/manage-overtimes?requestId=42", OvertimeNavigation.NotificationUrl("ActionRequired", type, OvertimeNavigation.StaffUrl, 42));
        Assert.Equal("/online/overtime-requests?requestId=42", OvertimeNavigation.NotificationUrl("RequestAction", type, OvertimeNavigation.StaffUrl, 42));
        Assert.Equal("/admin/attendance/manage-overtimes?requestId=42", OvertimeNavigation.NotificationUrl("ActionRequired", type, null, 42));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Review_actions_are_rendered_in_admin_only_and_applicant_cancellation_remains_available(bool admin, bool approved)
    {
        var service = Proxy.Create(approved);
        using var provider = new ServiceCollection().AddLogging().AddSingleton(service).AddSingleton<ToastService>()
            .AddSingleton<NavigationManager>(new Navigation(admin ? OvertimeNavigation.AdminUrl : OvertimeNavigation.StaffUrl)).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<OvertimeRequests>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["PendingOnly"] = true }))).ToHtmlString());
        if (admin) Assert.Contains("Approve stage", html);
        else { Assert.DoesNotContain("Approve stage", html); Assert.DoesNotContain(">Reject<", html); Assert.Contains("Review in Admin portal", html); Assert.Contains("manage-overtimes?requestId=42", html); }
        if(admin || !approved) Assert.Contains(">Cancel<", html); else Assert.DoesNotContain(">Cancel<", html);
    }

    private sealed class Navigation : NavigationManager
    {
        public Navigation(string path) => Initialize("https://localhost/", "https://localhost" + path);
        protected override void NavigateToCore(string uri, bool forceLoad) => throw new NotSupportedException();
    }
    public class Proxy : DispatchProxy
    {
        private bool approved;
        public static OvertimeRequestServiceContracts.IOperations Create(bool approved)
        {
            var proxy = Create<OvertimeRequestServiceContracts.IOperations, Proxy>();
            ((Proxy)(object)proxy).approved = approved;
            return proxy;
        }
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name switch
        {
            "LoadAsync" => Task.FromResult(new List<OvertimeRow> { new(42, approved ? 4 : 2, "OT-42", "Staff", new(2026, 10, 28), 60, approved ? "Approved" : "Pending", "Work", !approved, true, false, null) { CanReviseApproved = approved } }),
            "LoadWorkAsync" => Task.FromResult(new List<OvertimeWorkRow> { new(43, 2, "OT-43", "Staff", "Preapproval", new(2026, 10, 28), new(2026, 10, 28), "Work", [], "Pending", true, false, null, null) }),
            _ => throw new InvalidOperationException(method.Name)
        };
    }
}
