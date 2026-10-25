using HRM.Components.Admin.Settings;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.Services.Leave;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

static class RequestTransitionTests
{
    public static async Task RunAsync()
    {
        var options = new DbContextOptionsBuilder<HrmTeContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
        var factory = new ReceiptFactory(options);
        var user = new UserContextDto { UserId = 1, IndividualId = 1, IsSuperAdministrator = true };
        var isDeveloper = true;
        var access = Proxy.For<IUserAccessService>((name, _) => name switch
        {
            "RequireContextAsync" => Task.FromResult(user),
            "HasUserGroupAsync" => Task.FromResult(isDeveloper),
            "HasPermissionAsync" => Task.FromResult(true),
            _ => throw new Exception(name)
        });
        await using (var db = factory.CreateDbContext())
        {
            db.RequestTypes.AddRange(new RequestType { RequestTypeId = 100, TypeName = "Annual leave" }, new RequestType { RequestTypeId = 101, TypeName = "Other request" });
            db.RequestStates.AddRange(new RequestState { RequestStateId = 2, StateName = "Pending Verification", IsProcessingState = true },
                new RequestState { RequestStateId = 4, StateName = "Approved", IsFinalState = true },
                new RequestState { RequestStateId = 5, StateName = "Cancelled", IsFinalState = true },
                new RequestState { RequestStateId = 6, StateName = "Rejected", IsFinalState = true });
            db.Jobs.AddRange(new Job { JobId = 1, IndividualID = 1 }, new Job { JobId = 2, IndividualID = 2 });
            for (var i = 1; i <= 4; i++) db.Requests.Add(new Request { RequestId = i, RequestTypeId = 100, RequestStateId = i == 4 ? 4 : 2 });
            db.Leaves.AddRange(new Leaf { LeaveId = 1, JobId = 1, LeaveTypeId = 1, LeaveStateId = 2, Duration = 3 },
                new Leaf { LeaveId = 2, JobId = 2, LeaveTypeId = 1, LeaveStateId = 2, Duration = 3 },
                new Leaf { LeaveId = 4, JobId = 1, LeaveTypeId = 1, LeaveStateId = 4, Duration = 3 });
            db.LeaveRequests.AddRange(new LeaveRequest { RequestId = 1, LeaveId = 1 }, new LeaveRequest { RequestId = 2, LeaveId = 2 }, new LeaveRequest { RequestId = 4, LeaveId = 4 });
            db.JobLeaveTypes.Add(new JobLeaveType { JobLeaveTypeId = 1, JobId = 1, LeaveTypeId = 1, IsValid = true, RemainingDays = 10 });
            await db.SaveChangesAsync();
        }
        var settings = new RequestTransitionSettingsService(factory, access);
        var requests = new RequestService(factory, access, null!);
        var audits = 0;
        var logs = Proxy.For<IOperationLogService>((_, _) => Task.FromResult(new OperationLog { OperationLogId = ++audits }));
        var leaves = new LeaveService(factory, logs, null!, null!, access, null!, null!);
        var deniedCancel = await leaves.CancelLeaveAsync(1);
        Check(!deniedCancel.Success && deniedCancel.Message.Contains("contact your organisation's HR") && audits == 0, "Cancellation bypassed the transition table or wrote an audit before validation");
        await DeniedAsync(() => requests.ChangeStateAsync(3, 4));
        await using (var db = factory.CreateDbContext())
        {
            Check((await db.Requests.SingleAsync(x => x.RequestId == 3)).RequestStateId == 2 &&
                (await db.JobLeaveTypes.SingleAsync()).RemainingDays == 10, "Blocked transition changed a request or balance");
        }
        Check((await settings.SetAsync(100, 2, 5, true)).Success, "Could not configure cancellation");
        Check((await leaves.CancelLeaveAsync(1)).Success && audits == 1, "Allowed cancellation failed");
        Check(!(await leaves.CancelLeaveAsync(1)).Success && !(await leaves.CancelLeaveAsync(2)).Success && !(await leaves.CancelLeaveAsync(4)).Success,
            "Duplicate, other staff's or approved leave cancellation was allowed");
        await using (var db = factory.CreateDbContext()) Check((await db.JobLeaveTypes.SingleAsync()).RemainingDays == 13, "Cancelled leave refunded incorrectly");
        await settings.SetAsync(101, 2, 4, true);
        await DeniedAsync(() => requests.ChangeStateAsync(3, 4));
        await settings.SetAsync(100, 2, 4, true);
        await settings.SetAsync(100, 2, 4, true);
        Check((await settings.GetAsync(100)).Count(x => x.FromStateId == 2 && x.ToStateId == 4) == 1, "Duplicate transition was inserted");
        await settings.SetAsync(100, 2, 4, false);
        await DeniedAsync(() => requests.ChangeStateAsync(3, 4));
        await settings.SetAsync(100, 2, 6, true);
        Check(await requests.ChangeStateAsync(3, 6), "Allowed rejection failed");
        await settings.SetAsync(100, 6, 5, true);
        await DeniedAsync(() => requests.ChangeStateAsync(3, 5));
        Check(!(await settings.SetAsync(100, 2, 2, true)).Success, "Same-state transition was accepted");
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using (var renderer = new TestRenderer(services))
        {
            var page = new RequestStateTransitionSettings();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            typeof(RequestStateTransitionSettings).GetProperty("Configuration", flags)!.SetValue(page, settings);
            typeof(RequestStateTransitionSettings).GetProperty("Logger", flags)!.SetValue(page, NullLogger<RequestStateTransitionSettings>.Instance);
            await renderer.Dispatcher.InvokeAsync(async () => { renderer.Attach(page); await renderer.Render();
                await renderer.RootChange("transition-request-type", "100"); await renderer.RootChange("transition-from", "2");
                await renderer.RootChange("transition-to", "4"); await renderer.RootClick("Allow transition"); });
            renderer.CheckErrors();
            Check(renderer.RootText().Contains("Transition allowed"), "Configuration page could not add a transition");
            await renderer.Dispatcher.InvokeAsync(() => renderer.RootClick("Remove"));
            Check(renderer.RootText().Contains("Transition removed"), "Configuration page could not remove a transition");
        }
        isDeveloper = false;
        try { await settings.LoadAsync(); throw new Exception("Non-developer administrator can configure transitions"); }
        catch (UnauthorizedAccessException) { }
        try { await settings.GetAsync(100); throw new Exception("Non-developer can read transitions"); }
        catch (UnauthorizedAccessException) { }
        try { await settings.SetAsync(100, 2, 4, true); throw new Exception("Non-developer can add transitions"); }
        catch (UnauthorizedAccessException) { }
        try { await settings.SetAsync(100, 2, 5, false); throw new Exception("Non-developer can remove transitions"); }
        catch (UnauthorizedAccessException) { }
        await using (var renderer = new TestRenderer(services))
        {
            var page = new RequestStateTransitionSettings();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            typeof(RequestStateTransitionSettings).GetProperty("Configuration", flags)!.SetValue(page, settings);
            typeof(RequestStateTransitionSettings).GetProperty("Logger", flags)!.SetValue(page, NullLogger<RequestStateTransitionSettings>.Instance);
            await renderer.Dispatcher.InvokeAsync(async () => { renderer.Attach(page); await renderer.Render(); });
            renderer.CheckErrors();
            Check(renderer.RootText().Contains("Only the Developer user group") && renderer.RootButtonCount("Allow transition") == 0,
                "Direct page access exposed transition controls to a non-developer");
        }
    }
    static async Task DeniedAsync(Func<Task> action)
    {
        try { await action(); } catch (InvalidOperationException ex)
        { Check(ex is RequestTransitionConfigurationException || ex.Message.Contains("already final"), "Missing transition did not explain why it was blocked"); return; }
        throw new Exception("Disallowed state transition proceeded");
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
