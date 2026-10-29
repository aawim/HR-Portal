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
        html=await renderer.Dispatcher.InvokeAsync(async()=> (await renderer.RenderComponentAsync<PositionMatrixEditor>(ParameterView.Empty)).ToHtmlString());
        Assert.Contains("Approved staffing slots",html);Assert.DoesNotContain("Pay step<select",html);Assert.Contains("Effective from",html);
        Assert.DoesNotContain("could not be loaded",html);
    }
    public class Proxy:DispatchProxy
    {
        public bool Super{get;set;}
        protected override object? Invoke(MethodInfo? method,object?[]? args)=>method!.Name switch
        {
            "LoadPayFrameworksAsync"=>Task.FromResult(new PayFrameworkData([new(2,"NEW_PAY","New Pay Framework",true)],[],[new(1,"MVR")],Super,true)),
            _=>throw new InvalidOperationException(method.Name)
        };
    }
}
