using System.Reflection;
using HRM.Application.Contracts.Pages;
using HRM.Components.Admin.Settings.Positions;
using HRM.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HRM.UI.UnitTests;

public sealed class PositionAllowanceRenderingTests
{
    [Fact]
    public async Task Existing_rules_render_independent_end_date_inputs()
    {
        using var provider = new ServiceCollection().AddLogging()
            .AddSingleton(DispatchProxy.Create<IStaffFinance, FinanceProxy>())
            .AddSingleton<ToastService>().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<PositionAllowances>(ParameterView.Empty)).ToHtmlString());

        Assert.Contains("Phone", html);
        Assert.Contains("Transport", html);
        Assert.Contains("value=\"2026-11-30\"", html);
        Assert.Contains("value=\"2026-12-31\"", html);
        Assert.DoesNotContain("could not be loaded", html);
    }

    public class FinanceProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name switch
        {
            "GetConfigurationAsync" => Task.FromResult(new FinanceConfiguration([], [], [],
            [
                new(1, new(4, 1, 1, 100, 1, new(2026, 1, 1), new(2026, 11, 30)), "Executive", "Phone"),
                new(2, new(4, 2, 1, 200, 1, new(2026, 1, 1), new(2026, 12, 31)), "Executive", "Transport")
            ], [], true)),
            _ => throw new InvalidOperationException(method.Name)
        };
    }
}
