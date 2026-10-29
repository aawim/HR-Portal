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
    [Theory][InlineData(false)][InlineData(true)]
    public async Task Assign_position_is_available_only_when_no_active_position_remains(bool ended)
    {
        var service=DispatchProxy.Create<Contract.IOperations,Proxy>();((Proxy)(object)service).Ended=ended;
        using var provider=new ServiceCollection().AddLogging().AddSingleton(service).AddSingleton<ToastService>().BuildServiceProvider();await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
        var html=await renderer.Dispatcher.InvokeAsync(async()=> (await renderer.RenderComponentAsync<StaffPositions>(ParameterView.FromDictionary(new Dictionary<string,object?>{{"StaffId",50}}))).ToHtmlString());
        Assert.Equal(ended,html.Contains(">Assign position</button>",StringComparison.Ordinal));
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task Assign_and_change_position_filter_slots_by_framework_and_matrix_and_disable_full_matrices(bool assign)
    {
        var service=DispatchProxy.Create<Contract.IOperations,Proxy>();
        ((Proxy)(object)service).Ended=assign;
        using var provider=new ServiceCollection().AddLogging().AddSingleton(service).AddSingleton<ToastService>().BuildServiceProvider();
        await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
        var parameters=ParameterView.FromDictionary(new Dictionary<string,object?>{{"StaffId",50}});
        var html=await renderer.Dispatcher.InvokeAsync(async()=>assign?(await renderer.RenderComponentAsync<AssignForm>(parameters)).ToHtmlString():(await renderer.RenderComponentAsync<ChangeForm>(parameters)).ToHtmlString());
        Assert.Contains("Select framework",html);Assert.Contains("Select position matrix",html);Assert.Contains("Vacant position slot",html);Assert.Contains("1 / 5 slots available",html);Assert.Contains("Full",html);
        Assert.Matches("<option[^>]*value=\"2\"[^>]*disabled",html);Assert.Contains("Available slot",html);Assert.DoesNotContain("Custom slot",html);
    }
    public sealed class AssignForm:StaffPositions
    {
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();var type=typeof(StaffPositions);
            type.GetField("selectedFrameworkId",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(this,2);type.GetField("selectedMatrixId",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(this,1);
        }
    }
    [Fact]public async Task Ended_employment_explains_why_assign_position_is_unavailable()
    {
        var service=DispatchProxy.Create<Contract.IOperations,Proxy>();var proxy=(Proxy)(object)service;proxy.Ended=true;proxy.NoEmployment=true;
        using var provider=new ServiceCollection().AddLogging().AddSingleton(service).AddSingleton<ToastService>().BuildServiceProvider();await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
        var html=await renderer.Dispatcher.InvokeAsync(async()=> (await renderer.RenderComponentAsync<StaffPositions>(ParameterView.FromDictionary(new Dictionary<string,object?>{{"StaffId",50}}))).ToHtmlString());
        Assert.Contains("A new employment appointment must be approved",html);Assert.DoesNotContain(">Assign position</button>",html);
    }
    public sealed class ChangeForm:StaffPositions
    {
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();var type=typeof(StaffPositions);type.GetMethod("Edit",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(this,[Assignment,true,false]);
            type.GetField("selectedFrameworkId",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(this,2);type.GetField("selectedMatrixId",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(this,1);
        }
    }
    public class Proxy:DispatchProxy
    {
        public bool Ended{get;set;}
        public bool NoEmployment{get;set;}
        protected override object? Invoke(MethodInfo? method,object?[]? args)=>method!.Name switch
        {
            "LoadAsync"=>Task.FromResult(new Contract.Data(true,NoEmployment?[]:[new(5,"Job 5")],[new(10,"Available slot"){FrameworkId=2,MatrixId=1},new(11,"Custom slot"){FrameworkId=3,MatrixId=3}],[Ended?Assignment with{To=DateTime.UtcNow.AddDays(-1)}:Assignment]){TerminationRequestTypes=[new(22,"Job Resignation Request"),new(27,"Job Retirement Request")],Frameworks=[new(2,"NEW_PAY","Government",true),new(3,"CUSTOM","Custom",true)],PositionMatrices=[new(1,2,"Engineer",5,1),new(2,2,"Director",5,0),new(3,3,"Custom matrix",1,1)]}),
            "LoadRequestsAsync"=>Task.FromResult(new List<StaffPositionRequestRow>()),
            _=>throw new InvalidOperationException(method.Name)
        };
    }
}
