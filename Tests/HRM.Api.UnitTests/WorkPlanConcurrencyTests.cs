using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.WorkPlanning;
using HRM.WorkPlanning.Abstractions;
using HRM.WorkPlanning.Results;
using HRM.Api.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
namespace HRM.Api.UnitTests;

public sealed class WorkPlanConcurrencyTests
{
    [Fact]
    public async Task Same_plan_waits_but_other_jobs_and_tenants_do_not_block()
    {
        var gate=new WorkPlanGenerationGate();var day=new DateOnly(2026,10,29);
        using(var first=await gate.EnterAsync(10,1,day))
        {
            var pending=gate.EnterAsync(10,1,day);Assert.False(pending.IsCompleted);
            using(var otherJob=await gate.EnterAsync(10,2,day)){}
            using(var otherOrg=await gate.EnterAsync(20,1,day)){}
            first.Dispose();using(var second=await pending){}
        }
        Assert.Equal(0,gate.ActiveKeyCount);
    }
    [Fact]
    public async Task Cancelled_waiter_does_not_leak_or_release_another_owners_lock()
    {
        var gate=new WorkPlanGenerationGate();var day=new DateOnly(2026,10,29);
        using(var first=await gate.EnterAsync(10,1,day))
        {
            using var cancellation=new CancellationTokenSource();var waiting=gate.EnterAsync(10,1,day,cancellation.Token);cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>waiting);Assert.Equal(1,gate.ActiveKeyCount);
        }
        Assert.Equal(0,gate.ActiveKeyCount);using(var next=await gate.EnterAsync(10,1,day)){}Assert.Equal(0,gate.ActiveKeyCount);
    }
    [Fact]
    public async Task Concurrent_generation_creates_one_plan_and_one_set_of_assignments()
    {
        var factory=new QueryFactory(ignoreTransactions:true);var day=new DateOnly(2026,10,29);
        await using(var db=factory.CreateDbContext())
        {
            db.Jobs.Add(new(){JobId=10,IndividualID=200,OrganisationID=10,JobStateId=4,JoinedDate=new(2025,1,1)});
            db.WorkAssignmentStates.Add(new(){WorkAssignmentStateId=1,Code="PLANNED",Name="Planned",IsActive=true});
            db.WorkSegmentTypes.Add(new(){WorkSegmentTypeId=1,Code="CHECK_IN",Name="Check in",IsActive=true});
            db.WorkTemplates.Add(new(){WorkTemplateId=1,OrganisationBusinessEntityId=10,Name="Official",IsActive=true,DefaultStartTime=new(8,0),DefaultEndTime=new(14,0),WorkTemplateSegments=[new(){WorkTemplateSegmentId=1,WorkSegmentTypeId=1,Name="Check in",SequenceNumber=1,DurationMinutes=30,IsActive=true}]});
            db.JobWorkTemplateAssignments.Add(new(){JobID=10,WorkTemplateID=1,EffectiveFrom=new(2025,1,1),SchedulePurpose="Official",ScheduleMode="Weekly",WorkingDaysMask=127,IsActive=true});await db.SaveChangesAsync();
        }
        static async Task<PlanningProviderResolution> Resolve(){await Task.Yield();return new(){PlanningProviderId=1,Code="WORK_PLANNING"};}
        var provider=StubProxy.For<IPlanningProviderResolver>((_,_)=>Resolve());var planner=new WorkPlanGenerator(factory,provider);
        var plans=await Task.WhenAll(Enumerable.Range(0,12).Select(_=>planner.GenerateOrGetAsync(200,10,10,day)));
        Assert.Single(plans.Select(x=>x!.WorkPlanId).Distinct());
        await using(var db=factory.CreateDbContext()){Assert.Single(await db.WorkPlans.ToListAsync());Assert.Single(await db.WorkAssignments.ToListAsync());Assert.Single(await db.WorkPlanSegments.ToListAsync());}
        Assert.Equal(0,WorkPlanGenerationGate.Shared.ActiveKeyCount);
    }
    [Fact]
    public void Busy_generation_returns_retryable_status_without_exposing_internal_errors()
    {
        var context=new ExceptionContext(new ActionContext(new DefaultHttpContext(),new RouteData(),new ActionDescriptor()),new List<IFilterMetadata>()){Exception=new WorkPlanBusyException(new Exception("private database details"))};
        new ApiExceptionFilter(NullLogger<ApiExceptionFilter>.Instance).OnException(context);
        var result=Assert.IsType<ObjectResult>(context.Result);Assert.Equal(503,result.StatusCode);Assert.Equal("1",context.HttpContext.Response.Headers.RetryAfter.ToString());Assert.DoesNotContain("private",Assert.IsType<ProblemDetails>(result.Value).Title);
    }
}
