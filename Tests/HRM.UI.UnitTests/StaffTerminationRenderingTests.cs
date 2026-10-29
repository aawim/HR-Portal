using System.Reflection;
using HRM.Components.Admin.My.Workspace.Staff.Tabs;
using HRM.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Contract=HRM.Services.Contracts.StaffPositionManagementServiceContracts;

namespace HRM.UI.UnitTests;

public sealed class StaffTerminationRenderingTests
{
    internal static Contract.Assignment Assignment=>new(1,5,1,"Officer",DateTime.UtcNow.AddDays(-30),null,4,"Approved",1);
    [Fact]public async Task Active_position_termination_form_lists_job_request_types_and_effective_date()
    {
        var service=DispatchProxy.Create<Contract.IOperations,Proxy>();
        using var provider=new ServiceCollection().AddLogging().AddSingleton(service).AddSingleton<ToastService>().BuildServiceProvider();
        await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
        var html=await renderer.Dispatcher.InvokeAsync(async()=> (await renderer.RenderComponentAsync<TerminationForm>(ParameterView.FromDictionary(new Dictionary<string,object?>{{"StaffId",50}}))).ToHtmlString());
        Assert.Contains("Job termination request type",html);Assert.Contains("Job Resignation Request",html);Assert.Contains("Job Retirement Request",html);Assert.Contains("Termination effective date",html);
        Assert.DoesNotContain("End / remove",html);Assert.DoesNotContain("Position Removal Request",html);Assert.DoesNotContain("From date",html);Assert.Contains("Submit for approval",html);
    }
    public sealed class TerminationForm:StaffPositions
    {
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();typeof(StaffPositions).GetMethod("Edit",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(this,[Assignment,false,true]);
        }
    }
    public class Proxy:DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method,object?[]? args)=>method!.Name switch
        {
            "LoadAsync"=>Task.FromResult(new Contract.Data(true,[new(5,"Job 5")],[],[Assignment]){TerminationRequestTypes=[new(22,"Job Resignation Request"),new(27,"Job Retirement Request")]}),
            "LoadRequestsAsync"=>Task.FromResult(new List<StaffPositionRequestRow>()),
            _=>throw new InvalidOperationException(method.Name)
        };
    }
}
