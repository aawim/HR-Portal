using System.Reflection;
using HRM.Application.Contracts.Pages;
using HRM.Components.Leave;
using HRM.Components.Pages.Online.Leave.Forms;
using HRM.DTOs.Leave;
using HRM.DTOs.LeaveTypes;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.LeaveType;
using HRM.Services.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Xunit;
namespace HRM.UI.UnitTests;

public sealed class LeaveSubtypeRenderingTests
{
    [Fact]public async Task Settings_show_allocations_and_certificate_rules()
    {
        var service=DispatchProxy.Create<ILeaveTypeCatalogueService,Proxy>();
        using var provider=new ServiceCollection().AddLogging().AddSingleton(service).BuildServiceProvider();
        await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
        var html=await renderer.Dispatcher.InvokeAsync(async()=>(await renderer.RenderComponentAsync<LeaveSubtypeEditor>(ParameterView.FromDictionary(new Dictionary<string,object?>{{"LeaveTypeId",9}}))).ToHtmlString());
        Assert.Contains("30 days",html);Assert.Contains("With certificate",html);Assert.Contains("Without certificate",html);Assert.Contains("Save sub-types",html);
    }
    [Fact]public async Task Exhausted_without_certificate_form_blocks_submission_and_offers_upload()
    {
        var applications=DispatchProxy.Create<ILeaveApplications,Proxy>();var geography=DispatchProxy.Create<IGeographyService,Proxy>();var js=DispatchProxy.Create<IJSRuntime,Proxy>();
        using var provider=new ServiceCollection().AddLogging().AddSingleton(applications).AddSingleton(geography).AddSingleton(js).BuildServiceProvider();
        await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
        var html=await renderer.Dispatcher.InvokeAsync(async()=>(await renderer.RenderComponentAsync<FormHost>()).ToHtmlString());
        Assert.Contains("With certificate",html);Assert.Contains("Without certificate",html);Assert.Contains("Attach a certificate",html);
        Assert.Contains("type=\"file\"",html);Assert.Matches("type=\"submit\"[^>]*disabled",html);
    }
    public sealed class FormHost:ComponentBase
    {
        private readonly LeaveApplicationState state=new(){RemainingDays=15,Model=new(){JobLeaveTypeId=1,LeaveTypeId=9,LeaveReasonId=1,FromDate=new(2026,10,20),ToDate=new(2026,10,20),Duration=1}};
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b)
        {
            b.OpenComponent<EditForm>(0);b.AddAttribute(1,"Model",state.Model);b.AddAttribute(2,"ChildContent",(RenderFragment<EditContext>)(_=>c=>{c.OpenComponent<LeaveApplicationForm>(0);c.AddAttribute(1,"LeaveState",state);c.CloseComponent();}));b.CloseComponent();
        }
    }
    public class Proxy:DispatchProxy
    {
        protected override object? Invoke(MethodInfo? m,object?[]? args)=>m!.Name switch
        {
            "GetSubtypesAsync"=>Task.FromResult(new LeaveSubtypeSettings{LeaveTypeId=9,LeaveTypeName="Sick Leave",TotalDays=30,Subtypes=[new(){Id=1,Name="With certificate",Days=15,CertificateRule="Required"},new(){Id=2,Name="Without certificate",Days=15,CertificateRule="Without"}]}),
            "GetCountriesAsync"=>Task.FromResult(new List<Country>()),
            "GetApplicationLeaveTypeAsync"=>Task.FromResult<LeaveApplicationTypeDto?>(new(){LeaveTypeId=9,JobLeaveTypeId=1,RemainingDays=15,Reasons=[new(){Id=1,Name="General"}],Subtypes=[new(1,"With certificate",15,15,"Required"),new(2,"Without certificate",15,0,"Without")]}),
            "GetHandoverRulesAsync"=>Task.FromResult(new LeaveHandoverRulesDto(null,[])),
            _=>throw new InvalidOperationException(m.Name)
        };
    }
}
