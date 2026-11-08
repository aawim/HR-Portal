using HRM.Components.Admin.Hr.Staff;
using HRM.Components.Admin.My.Workspace;
using HRM.Enum;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HRM.UI.UnitTests;

public sealed class WorkspaceOnboardingNavigationTests
{
    [Theory]
    [InlineData("?individualId=820")]
    [InlineData("?requestId=23700")]
    public async Task Old_onboarding_links_keep_the_selected_person_or_request(string query)
    {
        var navigation=new TestNavigation("https://example.test/admin/hr/onboarding"+query);
        using var provider=new ServiceCollection().AddLogging().AddSingleton<NavigationManager>(navigation).BuildServiceProvider();
        await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async()=>await renderer.RenderComponentAsync<StaffOnboardingRedirect>());
        Assert.Equal("https://example.test/admin/my/workspace/onboarding"+query,navigation.Uri);
        Assert.True(navigation.Replaced);
    }
    [Fact]
    public async Task Main_workspace_offers_the_staff_onboarding_tab()
    {
        using var provider=new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
        var html=await renderer.Dispatcher.InvokeAsync(async()=>(await renderer.RenderComponentAsync<WorkspaceTabs>(ParameterView.FromDictionary(new Dictionary<string,object?>{{"EntityType",WorkspaceEntityType.Workspace},{"ActiveTab",WorkspaceTab.Onboarding}}))).ToHtmlString());
        Assert.Contains("Staff onboarding",html);Assert.Contains("aria-pressed=\"true\"",html);
        Assert.Equal("/admin/my/workspace/onboarding",HRM.Services.Contracts.StaffOnboardingServiceContracts.PageUrl);
    }
    private sealed class TestNavigation:NavigationManager
    {
        public bool Replaced{get;private set;}
        public TestNavigation(string uri)=>Initialize("https://example.test/",uri);
        protected override void NavigateToCore(string uri,bool forceLoad)=>Uri=ToAbsoluteUri(uri).ToString();
        protected override void NavigateToCore(string uri,NavigationOptions options){Replaced=options.ReplaceHistoryEntry;Uri=ToAbsoluteUri(uri).ToString();}
    }
}
