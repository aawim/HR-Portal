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

public sealed class PayFrameworkRenderingTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Framework_editor_is_reserved_for_superadministrator_and_matrix_form_renders(bool super)
    {
        var service=DispatchProxy.Create<IPositionManagement,Proxy>();((Proxy)(object)service).Super=super;
        using var provider=new ServiceCollection().AddLogging().AddSingleton(service).AddSingleton<ToastService>().BuildServiceProvider();
        await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
        var html=await renderer.Dispatcher.InvokeAsync(async()=> (await renderer.RenderComponentAsync<PayFrameworks>(ParameterView.Empty)).ToHtmlString());
        Assert.Contains("New Pay Framework",html);Assert.Contains("Job Matrices",html);
        Assert.Equal(super,html.Contains("Save framework",StringComparison.Ordinal));
        html=await renderer.Dispatcher.InvokeAsync(async()=> (await renderer.RenderComponentAsync<PositionMatrixEditor>(ParameterView.FromDictionary(new Dictionary<string,object?>{{"FrameworkId",2}}))).ToHtmlString());
        Assert.Contains("Approved staffing slots",html);Assert.DoesNotContain("Pay step<select",html);Assert.Contains("Effective from",html);
        Assert.Contains("Rank 7",html);Assert.Contains("Rank 10",html);Assert.Contains("Inactive",html);Assert.DoesNotContain("Other framework matrix",html);
        Assert.DoesNotContain("could not be loaded",html);
    }
    [Theory][InlineData("NEW_PAY")][InlineData("LEGACY")][InlineData("CUSTOM")]
    public async Task Position_creation_shows_framework_buttons_and_only_the_appropriate_form(string code)
    {
        var service=DispatchProxy.Create<IPositionManagement,Proxy>();((Proxy)(object)service).Code=code;
        using var provider=new ServiceCollection().AddLogging().AddSingleton(service).AddSingleton<ToastService>().BuildServiceProvider();
        await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
        var html=await renderer.Dispatcher.InvokeAsync(async()=> (await renderer.RenderComponentAsync<PositionGrid>(ParameterView.Empty)).ToHtmlString());
        Assert.Contains("aria-label=\"Position framework\"",html);Assert.Contains("New Pay Framework",html);Assert.Contains("Custom",html);Assert.Contains("Legacy CSC",html);
        Assert.Equal(code=="NEW_PAY",html.Contains("Create position matrix",StringComparison.Ordinal));
        Assert.Equal(code!="NEW_PAY",html.Contains("Save position",StringComparison.Ordinal));
        Assert.Equal(code=="LEGACY",html.Contains("Classification<select",StringComparison.Ordinal));
    }
    public class Proxy:DispatchProxy
    {
        public bool Super{get;set;}
        public string Code{get;set;}="NEW_PAY";
        private List<PositionFrameworkOption> Frameworks=>new List<PositionFrameworkOption>{new(2,"NEW_PAY","New Pay Framework",true),new(1,"LEGACY","Legacy CSC",true),new(3,"CUSTOM","Custom",true)}.OrderByDescending(x=>x.Code==Code).ToList();
        protected override object? Invoke(MethodInfo? method,object?[]? args)=>method!.Name switch
        {
            "LoadAsync"=>Task.FromResult(new PositionManagementData(Frameworks,[],[],true,Super)),
            "LoadPayFrameworksAsync"=>Task.FromResult(new PayFrameworkData(Frameworks,[new(1,2,"SW7","Software",1,100,new(2026,1,1),null,true,false,[],[],[]){Profile=new(){PublicServiceRank="7"}},new(2,2,"SW10","Software",1,100,new(2026,1,1),null,true,false,[],[],[]){Profile=new(){PublicServiceRank="10"}},new(3,2,"OLD","Software",1,100,new(2026,1,1),null,false,false,[],[],[]),new(4,3,"OTHER","Other framework matrix",1,100,new(2026,1,1),null,true,false,[],[],[])],[new(1,"MVR")],Super,true)),
            _=>throw new InvalidOperationException(method.Name)
        };
    }
}
