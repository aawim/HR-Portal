using System.Reflection;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Search=HRM.Components.Admin.Hr.Individual.IndividualSearchGrid;
static class UnattachedIndividualSearchTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static async Task RunAsync()
    {
        var factory=new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(),o=>o.EnableNullChecks(false)).Options);
        await using(var db=factory.CreateDbContext())
        {
            foreach(var id in new[]{1,2,3,4,5})db.Individuals.Add(new(){BusinessEntityId=id,FirstNameEnglish="Person",LastNameEnglish=id.ToString()});
            db.Staffs.Add(new(){IndividualId=5}); // A standalone staff identity does not constitute employment.
            db.Jobs.AddRange(new(){JobId=2,IndividualID=2,OrganisationID=10,JobStateId=4},new(){JobId=3,IndividualID=3,OrganisationID=20,JobStateId=4},new(){JobId=4,IndividualID=4,OrganisationID=10,JobStateId=4,TerminatedDate=DateTime.UtcNow.AddHours(5).Date.AddDays(-1)});
            db.Idcards.AddRange(new(){BusinessEntityId=1,IdcardNumber="FREE-ID"},new(){BusinessEntityId=2,IdcardNumber="STAFF-ID"});await db.SaveChangesAsync();
        }
        foreach(var super in new[]{false,true})
        {
            var access=Proxy.For<IUserAccessService>((m,a)=>m switch{"RequireContextAsync"=>Task.FromResult(new UserContextDto{UserId=1,IndividualId=99,IsSuperAdministrator=super}),"GetCurrentOrganisationIdAsync"=>Task.FromResult(10),"HasOrganisationAccessAsync" or "HasPermissionAsync"=>Task.FromResult(true),_=>throw new Exception(m)});
            using var services=new ServiceCollection().AddLogging().BuildServiceProvider();await using var renderer=new TestRenderer(services);var component=new Search();
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
            typeof(Search).GetProperty("DbFactory",flags)!.SetValue(component,factory);typeof(Search).GetProperty("UserAccess",flags)!.SetValue(component,access);
            var input=new Search.SearchModel{SearchType="Name",SearchTerm="Person"};typeof(Search).GetField("searchModel",flags)!.SetValue(component,input);
            await renderer.Dispatcher.InvokeAsync(async()=>{renderer.Attach(component);await renderer.Render();});
            await renderer.Dispatcher.InvokeAsync(()=>renderer.RootClick("Search"));renderer.CheckErrors();
            List<Search.IndividualResultDto> Results()=>(List<Search.IndividualResultDto>)typeof(Search).GetField("searchResults",flags)!.GetValue(component)!;
            Check(Results().Select(r=>r.BusinessEntityId).Order().SequenceEqual([1,5]),"Registry listed employed/previously attached people or hid an unattached identity");
            input.SearchType="ID";input.SearchTerm="STAFF-ID";
            await renderer.Dispatcher.InvokeAsync(()=>renderer.RootClick("Search"));Check(Results().Count==0,"National ID search bypassed the unattached-only filter");
            input.SearchTerm="FREE-ID";await renderer.Dispatcher.InvokeAsync(()=>renderer.RootClick("Search"));Check(Results().Single().BusinessEntityId==1,"Unattached national ID search failed");renderer.CheckErrors();
        }
    }
}
