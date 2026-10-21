using HRM.Components.Admin.My.Workspace;
using HRM.Components.Admin.My.Workspace.Cards;
using HRM.Components.Layout;
using HRM.Components.Pages.Online.Leave;
using HRM.DTOs;
using HRM.DTOs.Leave;
using HRM.Services;
using HRM.Services.Interfaces.Leave;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

static class WorkspaceLeaveChangesTests
{
    public static async Task RunAsync()
    {
        var early = new LeaveChangeRequestDto { RequestId = 42, Reference = "ER-42", Applicant = "Staff Member", ChangeType = "Early return", ChangeKind = 2,
            LeaveType = "Annual Leave", IsProcessing = true, Status = "Pending Approval", FromDate = new DateTime(2026, 7, 19), CurrentEndDate = new DateTime(2026, 7, 25),
            RequestedEndDate = new DateTime(2026, 7, 21), CurrentStage = "HR approval", PendingApprovers = ["HR Reviewer"],
            Actions = [new() { Label = "Approve", FromStateId = 3, ToStateId = 4 }] };
        var own = new LeaveChangeRequestDto { RequestId = 43, Reference = "OWN-43", ChangeType = "Early return", ChangeKind = 2, IsProcessing = true, IsOwnRequest = true,
            FromDate = new DateTime(2026, 7, 19), RequestedEndDate = new DateTime(2026, 7, 18), Actions = [new() { Label = "Cancel request", FromStateId = 3, ToStateId = 5 }] };
        var extension = new LeaveChangeRequestDto { RequestId = 44, Reference = "EXT-44", ChangeType = "Extension", ChangeKind = 1, IsProcessing = true };
        var closed = new LeaveChangeRequestDto { RequestId = 41, Reference = "CLOSED-41", IsProcessing = false };
        var failLoad = false;
        var executed = 0;
        var service = Proxy.For<ILeaveChangeService>((name, args) => name switch
        {
            "GetAssignedRequestsAsync" => failLoad ? throw new InvalidOperationException("Test unavailable") : Task.FromResult(new List<LeaveChangeRequestDto> { early, extension }),
            "GetMyRequestsAsync" => Task.FromResult(new List<LeaveChangeRequestDto> { early, own, closed }),
            "GetPendingLeaveRequestsAsync" => Task.FromResult(new List<PendingLeaveRequestDto>()),
            "ExecuteActionAsync" => Execute((int)args[0]!),
            _ => throw new Exception($"Unexpected service method {name}")
        });
        Task<ServiceResult> Execute(int id)
        {
            Check(id == 42, "Wrong change request processed");
            executed++;
            early.IsProcessing = false;
            return Task.FromResult(ServiceResult.Ok("Early return approved."));
        }
        using var services = new ServiceCollection().AddLogging().AddSingleton(service).AddSingleton<ToastService>().BuildServiceProvider();
        await using (var renderer = new TestRenderer(services))
        {
            var card = new TestPendingChanges();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            typeof(PendingLeaveChangesCard).GetProperty("Changes", flags)!.SetValue(card, service);
            typeof(PendingLeaveChangesCard).GetProperty("Toast", flags)!.SetValue(card, new ToastService());
            typeof(PendingLeaveChangesCard).GetProperty("Logger", flags)!.SetValue(card, NullLogger<PendingLeaveChangesCard>.Instance);
            await renderer.Dispatcher.InvokeAsync(async () => { renderer.Attach(card); await renderer.Render(); });
            renderer.CheckErrors();
            Check(renderer.RootButtonCount("ER-42") == 1 && renderer.RootText().Contains("EXT-44") && renderer.RootText().Contains("OWN-43") && !renderer.RootText().Contains("CLOSED-41"),
                "Pending changes lost early returns/extensions, duplicated a request, or showed final history");
            await renderer.Dispatcher.InvokeAsync(() => renderer.RootClick("ER-42"));
            Check(renderer.RootText().Contains("22 Jul 2026") && renderer.RootText().Contains("HR Reviewer"), "Early-return date or approver details incorrect");
            var buttons = (RequestActionButtons)renderer.Component(typeof(RequestActionButtons));
            await renderer.Dispatcher.InvokeAsync(() => buttons.OnExecute.InvokeAsync(new RequestActionSubmissionDto { FromStateId = 3, ToStateId = 4 }));
            renderer.CheckErrors();
            Check(executed == 1 && !renderer.RootText().Contains("ER-42"), "Approval did not use leave-change action handling or remove the completed request");
            await renderer.Dispatcher.InvokeAsync(() => renderer.RootClick("OWN-43"));
            Check(renderer.RootText().Contains("cannot verify or approve your own") && renderer.RootText().Contains("full leave period"), "Own-request guard or full cancellation preview missing");
            await renderer.Dispatcher.InvokeAsync(() => renderer.RootClick("Close change details"));
            failLoad = true;
            await renderer.Dispatcher.InvokeAsync(() => renderer.RootClick("Refresh changes"));
            Check(renderer.RootText().Contains("Leave changes could not be loaded"), "Load error was not explained");
        }
        failLoad = false;
        var activator = new WorkspaceChangeActivator();
        using var workspaceServices = new ServiceCollection().AddLogging().AddSingleton(service).AddSingleton<ToastService>()
            .AddSingleton<IComponentActivator>(activator).BuildServiceProvider();
        await using (var renderer = new TestRenderer(workspaceServices))
        {
            var workspace = new WorkspaceContentPage();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            typeof(WorkspaceContentPage).GetProperty("Leaves", flags)!.SetValue(workspace, service);
            typeof(WorkspaceContentPage).GetProperty("ToastService", flags)!.SetValue(workspace, new ToastService());
            typeof(WorkspaceContentPage).GetProperty("Logger", flags)!.SetValue(workspace, NullLogger<WorkspaceContentPage>.Instance);
            await renderer.Dispatcher.InvokeAsync(async () => { renderer.Attach(workspace); await renderer.Render(); });
            renderer.CheckErrors();
            Check(activator.Created.Contains(typeof(PendingLeaveChangesCard)), "Workspace did not mount the pending leave changes card");
        }
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
sealed class TestPendingChanges : PendingLeaveChangesCard
{
    protected override Task OnAfterRenderAsync(bool firstRender) => Task.CompletedTask;
}
sealed class WorkspaceChangeActivator : IComponentActivator
{
    public HashSet<Type> Created { get; } = [];
    public IComponent CreateInstance(Type type)
    {
        Created.Add(type);
        if (type == typeof(WorkSpaceLayout)) return new WorkSpaceLayout();
        if (type == typeof(PendingLeaveChangesCard)) return new TestPendingChanges();
        if (type == typeof(RequestActionButtons)) return new RequestActionButtons();
        return new EmptyWidget();
    }
}
