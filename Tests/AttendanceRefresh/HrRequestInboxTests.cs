using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.Workflow;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
static class HrRequestInboxTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static async Task RunAsync()
    {
        var factory=new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(),o=>o.EnableNullChecks(false)).Options);
        var wide=false;var current=10;
        var access=Proxy.For<IUserAccessService>((m,a)=>m switch{"RequireContextAsync"=>Task.FromResult(new UserContextDto{UserId=1,IndividualId=100}),"GetCurrentOrganisationIdAsync"=>Task.FromResult(current),"HasOrganisationAccessAsync"=>Task.FromResult(current>0),"HasPermissionAsync"=>Task.FromResult(wide&&(string)a[0]! =="Requests.View"),_=>throw new Exception(m)});
        var date=new DateTime(2026,10,26,9,0,0);
        await using(var db=factory.CreateDbContext())
        {
            db.Organisations.AddRange(new(){BusinessEntityID=10,OrganisationName="Current"},new(){BusinessEntityID=20,OrganisationName="Other"});
            db.Individuals.Add(new(){BusinessEntityId=100,FirstNameEnglish="Ahmed",MiddleNameEnglish="Ibrahim",LastNameEnglish="Afzal"});
            db.Users.Add(new(){UserId=50,BusinessEntityID=100,Username="afzal.login"});
            db.RequestTypes.AddRange(new(){RequestTypeId=1,TypeName="Supervisor",ViewUrl="/online/supervisor-requests"},new(){RequestTypeId=2,TypeName="Other"},new(){RequestTypeId=3,TypeName="Unsafe",ViewUrl="//external.example/path"});
            db.RequestStates.AddRange(new(){RequestStateId=2,StateName="Pending",IsProcessingState=true},new(){RequestStateId=4,StateName="Approved",IsFinalState=true});
            db.RequestStates.AddRange(new(){RequestStateId=1,StateName="Incomplete",IsProcessingState=true},new(){RequestStateId=5,StateName="Cancelled",IsFinalState=true},new(){RequestStateId=6,StateName="Rejected",IsFinalState=true},new(){RequestStateId=12,StateName="Pending HR Approval",IsProcessingState=true});
            db.WfWorkflowDefinitions.Add(new(){WorkflowDefinitionId=1,RequestTypeId=1,Name="Flow",Stages=[new(){WorkflowStageId=1,StageOrder=1,RequestStateId=2,StageName="HR"}]});
            for(var id=1;id<=26;id++)
            {
                db.Requests.Add(new(){RequestId=id,RequestTypeId=1,RequestStateId=id==26?4:2,OrganisationBusinessEntityID=10,ApplicantBusinessEntityId=100,ReferenceNumber=$"R-{id}",ApplicationDate=date});
                db.WfRequestInstances.Add(new(){WorkflowRequestInstanceId=id,WorkflowDefinitionId=1,RequestId=id});
                db.WfRequestStageInstances.Add(new(){RequestStageInstanceId=id,WorkflowRequestInstanceId=id,WorkflowStageId=1,Approvals=[new(){ApproverUserId=1}]});
            }
            foreach(var row in new[]{(30,2,10),(31,3,10),(32,1,20),(33,2,10)})db.Requests.Add(new(){RequestId=row.Item1,RequestTypeId=row.Item2,OrganisationBusinessEntityID=row.Item3,RequestStateId=row.Item1==31?5:2,ReferenceNumber=$"R-{row.Item1}",ApplicationDate=date.AddHours(row.Item1==30?1:0)});
            foreach(var id in new[]{30,31,32})db.RequestNotifications.Add(new(){RequestId=id,UserId=1,FromStateId=2,ToStateId=2});
            await db.SaveChangesAsync();
        }
        var service=new HrRequestInboxService(factory,access);
        var all=await service.LoadAsync();
        Check(all.TypeId==0&&all.Items.Count==25&&all.TotalPages==2,"Default view did not include all request types");
        Check(all.Items.Take(3).Select(r=>r.Id).SequenceEqual([30,31,26]),"Requests were not newest first across types with a stable ID tie-break");
        Check((await service.LoadAsync(0,2)).Items.Count==3,"All-request pagination lost requests");
        Check((await service.LoadAsync(1,status:"5")).Items.Count==0,"An empty type/status filter switched to another request type");
        var inbox=await service.LoadAsync(1);
        Check(!inbox.OrganisationWide&&inbox.Groups.Sum(g=>g.Count)==28,"Inbox leaked unreceived or foreign requests");
        Check(inbox.Items.Count==25&&inbox.TotalPages==2&&inbox.Groups.Single(g=>g.Id==1).Count==26,"Type grouping or pagination truncated requests");
        Check((await service.LoadAsync(1,2)).Items.Count==1,"Second page lost request");
        Check(inbox.Items.All(r=>r.Url!.StartsWith("/online/supervisor-requests?requestId=")&&r.AppliedAt.Hour==14),"Request links or Maldives time are wrong");
        Check((await service.LoadAsync(3)).Items.Single().Url==null,"Untrusted request URL was allowed");
        Check((await service.LoadAsync(status:"final")).Groups.Sum(g=>g.Count)==2,"Final-state filter failed");
        Check(inbox.Statuses.Count==6&&inbox.Statuses.Any(s=>s.Id==12)&&inbox.Statuses.Any(s=>s.Id==6),"Statuses with no current requests were missing from the filter");
        Check((await service.LoadAsync(status:"4")).Items.Single().Id==26,"Approved status did not filter exactly");
        Check((await service.LoadAsync(status:"5")).Items.Single().Id==31,"Cancelled status did not filter exactly");
        var emptyState=await service.LoadAsync(status:"6");Check(emptyState.Groups.Count==0&&emptyState.Statuses.Count==6,"Empty status results removed filter choices");
        try{await service.LoadAsync(status:"9999");throw new Exception("Unknown status accepted");}catch(InvalidOperationException ex) when(ex.Message=="Select a valid request status."){}
        Check((await service.LoadAsync(search:"Ahmed Afzal")).Groups.Sum(g=>g.Count)==26,"Full-name search failed with a middle name");
        Check((await service.LoadAsync(search:"Ibrahim")).Groups.Sum(g=>g.Count)==26,"Middle-name search failed");
        Check((await service.LoadAsync(search:"afzal.login")).Groups.Sum(g=>g.Count)==26,"Account username search failed");
        Check((await service.LoadAsync(1,search:"Ahmed Afzal",status:"4",from:date.Date,to:date.Date)).Items.Single().Id==26,"User, request type, status and date filters did not combine");
        Check((await service.LoadAsync(1,search:"no-match")).Types.Count==3,"Empty results removed request type choices");
        Check((await service.LoadAsync(search:"R-30")).Items.Single().Id==30,"Reference search failed");
        Check((await service.LoadAsync(from:date.Date,to:date.Date)).Groups.Sum(g=>g.Count)==28,"Inclusive business-date filter failed");
        Check((await service.LoadAsync(from:date.Date.AddDays(1))).Groups.Count==0,"Date filter included earlier requests");
        Check((await service.LoadAsync(status:"5",from:date.Date)).Items.Single().Id==31,"Status plus From date did not combine");
        Check((await service.LoadAsync(search:"R-30",status:"2",from:date.Date,to:date.Date)).Items.Single().Id==30,"Search, status and both dates did not combine");
        Check((await service.LoadAsync(status:"5",from:date.Date.AddDays(1))).Items.Count==0,"From date did not further restrict selected status");
        using(var canceled=new CancellationTokenSource()){canceled.Cancel();try{await service.LoadAsync(cancellationToken:canceled.Token);throw new Exception("Canceled query completed");}catch(OperationCanceledException){}}
        await CheckLiveFilters(service);
        wide=true;Check((await service.LoadAsync()).OrganisationWide&&(await service.LoadAsync()).Groups.Sum(g=>g.Count)==29,"Organisation viewer could not see all current-tenant requests");
        current=20;Check((await service.LoadAsync()).Groups.Sum(g=>g.Count)==1,"Current organisation switch leaked the prior tenant");
        current=0;try{await service.LoadAsync();throw new Exception("Missing context accepted");}catch(UnauthorizedAccessException){}
    }
    static async Task CheckLiveFilters(HrRequestInboxService service)
    {
        using var services=new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer=new TestRenderer(services);
        var component=new HRM.Components.Admin.Hr.Group.HrRequestsInbox();
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        component.GetType().GetProperty("Requests",flags)!.SetValue(component,service);
        component.GetType().GetProperty("Logger",flags)!.SetValue(component,Microsoft.Extensions.Logging.Abstractions.NullLogger<HRM.Components.Admin.Hr.Group.HrRequestsInbox>.Instance);
        await renderer.Dispatcher.InvokeAsync(async()=>{renderer.Attach(component);await renderer.Render();});
        Check(renderer.RootButtonCount("Search")==0&&renderer.RootButtonCount("Reset")==1,"Filter row still requires a Search button");
        await renderer.Dispatcher.InvokeAsync(()=>renderer.RootChange("hr-request-type","1"));
        Check(!renderer.RootText().Contains("R-30")&&renderer.RootText().Contains("R-26"),"Request type dropdown did not filter immediately");
        await renderer.Dispatcher.InvokeAsync(()=>renderer.RootEvent("oninput",new ChangeEventArgs{Value="afzal.login"}));
        Check(renderer.RootText().Contains("R-26")&&!renderer.RootText().Contains("R-30"),"Username did not combine with selected type");
        await renderer.Dispatcher.InvokeAsync(()=>renderer.RootClick("Reset"));
        await renderer.Dispatcher.InvokeAsync(()=>renderer.RootChange("hr-request-status","5"));
        Check(renderer.RootText().Contains("R-31")&&!renderer.RootText().Contains("R-30"),"Status selection did not filter immediately");
        await renderer.Dispatcher.InvokeAsync(()=>renderer.RootChange("hr-request-from","2026-10-27"));
        Check(renderer.RootText().Contains("No matching received requests."),"From date did not combine with selected status immediately");
        await renderer.Dispatcher.InvokeAsync(()=>renderer.RootClick("Reset"));
        await renderer.Dispatcher.InvokeAsync(()=>renderer.RootChange("hr-request-status","2"));
        await renderer.Dispatcher.InvokeAsync(()=>renderer.RootChange("hr-request-from","2026-10-26"));
        await renderer.Dispatcher.InvokeAsync(()=>renderer.RootChange("hr-request-to","2026-10-26"));
        await renderer.Dispatcher.InvokeAsync(()=>renderer.RootEvent("oninput",new ChangeEventArgs{Value="R-30"}));
        Check(renderer.RootText().Contains("R-30")&&!renderer.RootText().Contains("R-31"),"Live search did not combine with both dates and status");
        var older=renderer.Dispatcher.InvokeAsync(()=>renderer.RootEvent("oninput",new ChangeEventArgs{Value="R-31"}));
        var newer=renderer.Dispatcher.InvokeAsync(()=>renderer.RootEvent("oninput",new ChangeEventArgs{Value="R-30"}));
        await Task.WhenAll(older,newer);
        Check(renderer.RootText().Contains("R-30")&&!renderer.RootText().Contains("No matching received requests."),"Superseded live search overwrote the latest result");
        await renderer.Dispatcher.InvokeAsync(()=>renderer.RootClick("Reset"));
        Check(renderer.RootText().Contains("R-31")&&renderer.RootText().Contains("R-30"),"Reset did not clear all filters");
        renderer.CheckErrors();
    }

}
