using System.Reflection;
using HRM.DTOs;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Search = HRM.Components.Admin.My.Workspace.WorkspaceSearch;

static class SearchReliabilityTests
{
    static void Check(bool ok,string message) { if(!ok)throw new Exception(message); }
    public static async Task RunAsync()
    {
        var factory=new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(),o=>o.EnableNullChecks(false)).Options);
        var today=DateTime.UtcNow.AddHours(5).Date;
        await using(var db=factory.CreateDbContext())
        {
            db.Individuals.AddRange(new(){BusinessEntityId=1,FirstNameEnglish="Ahmed",LastNameEnglish="Afzal"},new(){BusinessEntityId=2,FirstNameEnglish="Ended",LastNameEnglish="Person"},new(){BusinessEntityId=3,FirstNameEnglish="Foreign",LastNameEnglish="Person"});
            db.Jobs.AddRange(new(){JobId=1,IndividualID=1,OrganisationID=10,JobStateId=4,JoinedDate=today.AddDays(-5)},new(){JobId=2,IndividualID=2,OrganisationID=10,JobStateId=4,JoinedDate=today.AddDays(-5)},new(){JobId=3,IndividualID=3,OrganisationID=20,JobStateId=4,JoinedDate=today.AddDays(-5)});
            db.JobPositions.AddRange(new(){JobPositionId=1,JobId=1,JobPositionStateId=4,FromDate=today.AddDays(-5)},new(){JobPositionId=2,JobId=2,JobPositionStateId=4,FromDate=today.AddDays(-5),ToDate=today},new(){JobPositionId=3,JobId=3,JobPositionStateId=4,FromDate=today.AddDays(-5)});
            await db.SaveChangesAsync();
        }
        var org=10;var orgReads=0;var switchOrganisation=false;TaskCompletionSource<UserContextDto>? gate=null;
        var actor=new UserContextDto{UserId=1,IndividualId=1};
        var access=Proxy.For<IUserAccessService>((m,a)=>m switch {"RequireContextAsync"=>gate?.Task??Task.FromResult(actor),"GetCurrentOrganisationIdAsync"=>Task.FromResult(switchOrganisation&&++orgReads>1?20:org),"HasPermissionAsync"=>Task.FromResult(false),_=>throw new Exception(m)});
        using var services=new ServiceCollection().AddLogging().BuildServiceProvider();await using var renderer=new TestRenderer(services);
        var search=new Search{StaffOnly=true,RequireActivePosition=true};
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        typeof(Search).GetProperty("_dbFactory",flags)!.SetValue(search,factory);
        typeof(Search).GetProperty("userAccessService",flags)!.SetValue(search,access);
        typeof(Search).GetProperty("ToastService",flags)!.SetValue(search,new ToastService());
        await renderer.Dispatcher.InvokeAsync(async()=>{renderer.Attach(search);await renderer.Render();});
        Task Query(string text,int version)
        {
            typeof(Search).GetField("_searchText",flags)!.SetValue(search,text);
            typeof(Search).GetField("searchVersion",flags)!.SetValue(search,version);
            return (Task)typeof(Search).GetMethod("SearchAsync",flags)!.Invoke(search,[version,CancellationToken.None])!;
        }
        List<WorkspaceSearchResult> Results()=>(List<WorkspaceSearchResult>)typeof(Search).GetField("StaffResults",flags)!.GetValue(search)!;
        await renderer.Dispatcher.InvokeAsync(()=>Query("Ahmed   Afzal",1));
        Check(Results().Count==1&&Results()[0].Name=="Ahmed Afzal","Missing middle name or input whitespace broke full-name search");
        await renderer.Dispatcher.InvokeAsync(()=>Query("Ended",2));Check(Results().Count==0,"Position ending today remained selectable");
        await renderer.Dispatcher.InvokeAsync(()=>Query("Foreign",3));Check(Results().Count==0,"Staff search crossed tenants");
        switchOrganisation=true;orgReads=0;await renderer.Dispatcher.InvokeAsync(()=>Query("Ahmed",4));Check(Results().Count==0,"Search published results after organisation changed");switchOrganisation=false;
        gate=new(TaskCreationOptions.RunContinuationsAsynchronously);
        var older=renderer.Dispatcher.InvokeAsync(()=>Query("Ahmed",4));await Task.Delay(300);
        var release=gate;gate=null;
        await renderer.Dispatcher.InvokeAsync(()=>Query("No result",5));release.SetResult(actor);await older;
        Check(Results().Count==0,"Older search overwrote the newer result");
        await renderer.Dispatcher.InvokeAsync(()=>{typeof(Search).GetProperty("SearchText",flags)!.SetValue(search,"Ahmed");typeof(Search).GetProperty("SearchText",flags)!.SetValue(search,"");});
        await Task.Delay(300);Check(Results().Count==0,"Canceled search repopulated the cleared input");
        renderer.CheckErrors();Console.WriteLine("PASS Staff search: whitespace, missing middle name, active-position boundary, tenant isolation, stale responses and debounce cancellation");
    }
}
