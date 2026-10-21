using System.Reflection;
using HRM.Components.Pages.Online.Dashboard;
using HRM.Components.Pages.Online.Leave;
using HRM.Components.Shared;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.LeaveTypes;
using HRM.Models.Workflow;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.Leave;
using HRM.Services.Interfaces.AppovalWF;
using HRM.Services.Leave;
using Microsoft.AspNetCore.Components;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

static class LeaveHistoryTests
{
    public static async Task RunAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<HrmTeContext>().UseSqlite(connection).Options;
        var factory = new HistoryFactory(options);
        var start = DateTime.Today.AddDays(-30);
        await using (var db = factory.CreateDbContext())
        {
            await db.Database.EnsureCreatedAsync();
            db.Jobs.AddRange(new Job { JobId = 1, IndividualID = 1 },
                new Job { JobId = 2, IndividualID = 1, TerminatedDate = DateTime.Today.AddDays(-60) },
                new Job { JobId = 3, IndividualID = 2 });
            db.LeaveTypes.Add(new LeaveType { LeaveTypeId = 1, Name = "Annual Leave" });
            db.LeaveStates.AddRange(new LeaveState { LeaveStateId = 2, StateName = "Pending verification" },
                new LeaveState { LeaveStateId = 4, StateName = "Approved" },
                new LeaveState { LeaveStateId = 5, StateName = "Rejected" },
                new LeaveState { LeaveStateId = 6, StateName = "Cancelled" });
            for (var i = 1; i <= 16; i++)
            {
                var from = i == 16 ? start.AddYears(-1) : start.AddDays(i);
                db.Leaves.Add(new Leaf { LeaveId = i, JobId = i == 16 ? 2 : 1, LeaveTypeId = 1,
                    FromDate = from, ToDate = from.AddDays(2), Duration = 3,
                    LeaveStateId = i == 1 ? 5 : i == 3 ? 6 : i % 2 == 0 ? 4 : 2 });
            }
            db.Leaves.Add(new Leaf { LeaveId = 99, JobId = 3, LeaveTypeId = 1,
                FromDate = start.AddDays(5), ToDate = start.AddDays(7), LeaveStateId = 4 });
            await db.SaveChangesAsync();
        }
        var access = Proxy.For<IUserAccessService>((_, _) => Task.FromResult(new UserContextDto
            { IndividualId = 1, ActiveJob = new ActiveJobDto { JobId = 1 } }));
        var service = new LeaveService(factory, null!, null!, null!, access, null!, null!);
        var recent = await service.GetMyLeaveRequestsAsync(limit: 10);
        Check(recent.Select(x => x.LeaveId).SequenceEqual(Enumerable.Range(7, 10).Reverse()), "Recent limit/order incorrect, or another employee's leave leaked");
        Check(recent.Any(x => x.LeaveStateId == 4) && recent.Any(x => x.JobId == 2), "Approved leave or previous employment leave is missing");
        Check((await service.GetLeaveRequestsAsync(1)).Any(x => x.LeaveStateId == 4), "Job leave query still excludes approved leave");
        Check((await service.GetMyLeaveRequestsAsync(fromDate: start.AddDays(5), toDate: start.AddDays(5)))
            .Select(x => x.LeaveId).SequenceEqual(new[] { 5, 4, 3 }), "Inclusive date overlap search is incorrect");
        Check((await service.GetMyLeaveRequestsAsync(4, start.AddDays(5), start.AddDays(7)))
            .Select(x => x.LeaveId).SequenceEqual(new[] { 6, 4 }), "Combined approved/date search is incorrect");
        Check((await service.GetMyLeaveRequestsAsync()).Count == 16, "View all query is limited or ownership filtering failed");

        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using (var renderer = new TestRenderer(services))
        {
            var card = Card(service, factory, access);
            await renderer.Dispatcher.InvokeAsync(async () => { renderer.Attach(card); await renderer.Render(); });
            renderer.CheckErrors();
            Check(Rows(card).Count == 10 && renderer.RootText().Contains("Approved") && renderer.RootText().Contains("View all"),
                "Dashboard does not render approved leaves and View all in the latest 10");
            Check(renderer.RootButtonCount("Cancel") == 5, "Approved leaves show unavailable cancellation actions");
            Check(renderer.RootText().Contains("View receipt"), "Approved leaves do not offer receipts");
            Check(!renderer.RootText().Contains("could not be loaded"), "Recent workflow metadata failed to load");
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(RecentLeaveRequestsCard).GetField("LeaveRequestIds", flags)!.SetValue(card, new Dictionary<int, int> { [15] = 100 });
            typeof(RecentLeaveRequestsCard).GetField("RequestStatuses", flags)!.SetValue(card, new Dictionary<int, string> { [100] = "Pending HR verification" });
            typeof(RecentLeaveRequestsCard).GetField("ProcessingRequests", flags)!.SetValue(card, new Dictionary<int, bool> { [100] = true });
            await renderer.Dispatcher.InvokeAsync(() => renderer.Render());
            var status = (string)typeof(RecentLeaveRequestsCard).GetMethod("GetDisplayStatus", flags)!.Invoke(card, [Rows(card).Single(x => x.LeaveId == 15)])!;
            Check(status == "Pending verification" && renderer.RootText().Contains("Pending HR verification"), "Displayed status does not match its filter, or workflow stage was lost");
            // Processing is visible only for non-final leaves, even with stale linked request flags.
            var workflowType = typeof(RecentLeaveRequestsCard).GetNestedType("WorkflowCardInfo", BindingFlags.NonPublic)!;
            var workflow = Activator.CreateInstance(workflowType, nonPublic: true)!;
            workflowType.GetProperty("WorkflowStatus")!.SetValue(workflow, "InProgress");
            workflowType.GetProperty("StageName")!.SetValue(workflow, "Processing annual leave");
            var workflowMap = (System.Collections.IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(int), workflowType))!;
            workflowMap.Add(101, workflow);
            typeof(RecentLeaveRequestsCard).GetField("WorkflowInfo", flags)!.SetValue(card, workflowMap);
            typeof(RecentLeaveRequestsCard).GetField("LeaveRequestIds", flags)!.SetValue(card, new Dictionary<int, int> { [15] = 101 });
            typeof(RecentLeaveRequestsCard).GetField("RequestStatuses", flags)!.SetValue(card, new Dictionary<int, string> { [101] = "Processing" });
            typeof(RecentLeaveRequestsCard).GetField("ProcessingRequests", flags)!.SetValue(card, new Dictionary<int, bool> { [101] = true });
            await renderer.Dispatcher.InvokeAsync(() => renderer.Render());
            Check(renderer.RootText().Contains("Processing annual leave"), "Active processing stage disappeared for pending leave");
            var pendingLeave = Rows(card).Single(x => x.LeaveId == 15);
            foreach (var finalState in new[] { 4, 5, 6 })
            {
                pendingLeave.LeaveStateId = finalState;
                await renderer.Dispatcher.InvokeAsync(() => renderer.Render());
                Check(!renderer.RootText().Contains("Processing annual leave") && !renderer.RootText().Contains("Processing ·"),
                    "Final legacy leave displays a stale processing request");
            }
            pendingLeave.LeaveStateId = 2;
            workflowType.GetProperty("Completed")!.SetValue(workflow, true);
            typeof(RecentLeaveRequestsCard).GetField("ProcessingRequests", flags)!.SetValue(card, new Dictionary<int, bool> { [101] = false });
            await renderer.Dispatcher.InvokeAsync(() => renderer.Render());
            Check(!renderer.RootText().Contains("Processing annual leave"), "Completed processing stage is still displayed");
            await renderer.Dispatcher.InvokeAsync(() => renderer.RootClick("View"));
            renderer.CheckErrors();
            Check(renderer.RootText().Contains("Leave #16"), "Approved leave without a linked request cannot be viewed");
        }
        await using (var renderer = new TestRenderer(services))
        {
            var card = Card(service, factory, access);
            await renderer.Dispatcher.InvokeAsync(async () =>
            {
                renderer.Attach(card);
                await renderer.Render(ParameterView.FromDictionary(new Dictionary<string, object?> { ["ShowAll"] = true }));
                await renderer.RootSelect("4");
                await renderer.RootChange("leave-history-from", start.AddDays(5).ToString("yyyy-MM-dd"));
                await renderer.RootChange("leave-history-to", start.AddDays(7).ToString("yyyy-MM-dd"));
                await renderer.RootClick("Search");
            });
            renderer.CheckErrors();
            Check(Rows(card).Select(x => x.LeaveId).SequenceEqual(new[] { 6, 4 }), "View all status/date form does not filter results");
            await renderer.Dispatcher.InvokeAsync(async () =>
            {
                await renderer.RootChange("leave-history-from", start.AddDays(8).ToString("yyyy-MM-dd"));
                await renderer.RootClick("Search");
            });
            Check(renderer.RootText().Contains("From date must be on or before To date"), "Invalid date range is not explained");
            await renderer.Dispatcher.InvokeAsync(() => renderer.RootClick("Reset filters"));
            renderer.CheckErrors();
            Check(Rows(card).Count == 16 && !renderer.RootText().Contains("could not be loaded"), "Reset did not restore all leave records");
        }
        var activator = new HistoryPageActivator();
        using var pageServices = new ServiceCollection().AddLogging()
            .AddSingleton<IComponentActivator>(activator)
            .AddSingleton<ILeaveService>(service).AddSingleton<IUserAccessService>(access)
            .AddSingleton<IDbContextFactory<HrmTeContext>>(factory).AddSingleton<ToastService>()
            .AddSingleton(Proxy.For<ILeaveChangeService>((name, _) => throw new Exception(name)))
            .AddSingleton(Proxy.For<IWorkflowEngine>((name, _) => throw new Exception(name)))
            .BuildServiceProvider();
        await using (var renderer = new TestRenderer(pageServices))
        {
            await renderer.Dispatcher.InvokeAsync(async () =>
            {
                renderer.Attach(new AllLeaveRequests());
                await renderer.Render();
            });
            renderer.CheckErrors();
            var card = (HistoryCard)renderer.Component(typeof(HistoryCard));
            Check(card.ShowAll && Rows(card).Count == 16 && activator.Created.Contains(typeof(BackButton)),
                "View all page does not mount the full history and Back to Dashboard components");
        }
    }
    public static async Task ProcessingVisibilityAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var factory = new HistoryFactory(new DbContextOptionsBuilder<HrmTeContext>().UseSqlite(connection).Options);
        await using (var db = factory.CreateDbContext())
        {
            await db.Database.EnsureCreatedAsync();
            db.Jobs.AddRange(new Job { JobId = 1, IndividualID = 1 }, new Job { JobId = 2, IndividualID = 2 });
            db.LeaveTypes.Add(new LeaveType { LeaveTypeId = 1, Name = "Annual Leave" });
            db.LeaveStates.AddRange(new LeaveState { LeaveStateId = 2, StateName = "Pending verification" },
                new LeaveState { LeaveStateId = 4, StateName = "Approved" },
                new LeaveState { LeaveStateId = 5, StateName = "Rejected" },
                new LeaveState { LeaveStateId = 6, StateName = "Cancelled" });
            db.RequestStates.AddRange(new RequestState { RequestStateId = 2, StateName = "Pending verification", IsProcessingState = true },
                new RequestState { RequestStateId = 4, StateName = "Approved", IsProcessingState = true, IsFinalState = true });
            for (var i = 1; i <= 6; i++)
            {
                db.Leaves.Add(new Leaf { LeaveId = i, JobId = i == 3 ? 2 : 1, LeaveTypeId = 1,
                    LeaveStateId = i >= 4 ? i : i == 2 ? 4 : 2,
                    FromDate = new DateTime(2026, 7, 19), ToDate = new DateTime(2026, 7, 19), Duration = 1 });
                db.Requests.Add(new Request { RequestId = i, RequestStateId = i == 2 ? 4 : 2 });
                db.LeaveRequests.Add(new LeaveRequest { LeaveId = i, RequestId = i });
                if (i <= 3) db.WfRequestInstances.Add(new WfRequestInstance { WorkflowRequestInstanceId = i, RequestId = i,
                    WorkflowStatus = i == 2 ? "Approved" : "InProgress", CompletedAt = i == 2 ? DateTime.UtcNow : null });
            }
            await db.SaveChangesAsync();
        }
        var access = Proxy.For<IUserAccessService>((_, _) => Task.FromResult(new UserContextDto
            { IndividualId = 1, ActiveJob = new ActiveJobDto { JobId = 99 } }));
        var service = new LeaveService(factory, null!, null!, null!, access, null!, null!);
        var processing = await service.GetMyProcessingLeavesAsync();
        Check(processing.Count == 1 && processing[0].FromDate == new DateTime(2026, 7, 19),
            "July leave from previous job is missing, or completed/another employee's leave leaked");
    }

    private static HistoryCard Card(LeaveService leaveService, HistoryFactory factory, IUserAccessService access)
    {
        var card = new HistoryCard();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        void Set(string property, object value) => typeof(RecentLeaveRequestsCard).GetProperty(property, flags)!.SetValue(card, value);
        Set("LeaveService", leaveService); Set("DbFactory", factory); Set("UserAccess", access);
        Set("ToastService", new ToastService()); Set("Logger", NullLogger<RecentLeaveRequestsCard>.Instance);
        return card;
    }
    private static List<Leaf> Rows(HistoryCard card) =>
        (List<Leaf>)typeof(RecentLeaveRequestsCard).GetField("LeaveRequests", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(card)!;
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
}

sealed class HistoryPageActivator : IComponentActivator
{
    public HashSet<Type> Created { get; } = [];
    public IComponent CreateInstance(Type type)
    {
        Created.Add(type);
        return type == typeof(RecentLeaveRequestsCard) ? new HistoryCard() : new EmptyWidget();
    }
}

sealed class HistoryCard : RecentLeaveRequestsCard
{
    protected override Task OnAfterRenderAsync(bool firstRender) => Task.CompletedTask;
}
sealed class HistoryFactory(DbContextOptions<HrmTeContext> options) : IDbContextFactory<HrmTeContext>
{
    public HrmTeContext CreateDbContext() => new HistoryContext(options);
}
sealed class HistoryContext(DbContextOptions<HrmTeContext> options) : HrmTeContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        Type[] types = [typeof(Job), typeof(Leaf), typeof(LeaveType), typeof(LeaveState), typeof(LeaveRequest), typeof(Request), typeof(RequestState), typeof(LeaveHandoverReview), typeof(WfRequestInstance)];
        var navigations = new HashSet<(Type, string)>
        {
            (typeof(Leaf), "Job"), (typeof(Leaf), "LeaveType"), (typeof(Leaf), "LeaveState"),
            (typeof(LeaveRequest), "Leave"), (typeof(LeaveRequest), "Request"), (typeof(Request), "RequestState")
        };
        foreach (var type in builder.Model.GetEntityTypes().Select(x => x.ClrType).ToArray())
            if (!types.Contains(type)) builder.Ignore(type);
        foreach (var type in types)
        {
            builder.Entity(type).HasKey(type == typeof(WfRequestInstance) ? "WorkflowRequestInstanceId" : type == typeof(Leaf) || type == typeof(LeaveHandoverReview) ? "LeaveId" : type == typeof(LeaveRequest) ? "RequestId" : type.Name + "Id");
            foreach (var property in type.GetProperties().Where(x => !x.PropertyType.IsValueType && x.PropertyType != typeof(string) && x.PropertyType != typeof(byte[])))
                if (!navigations.Contains((type, property.Name))) builder.Entity(type).Ignore(property.Name);
        }
        builder.Entity<Leaf>().HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId);
        builder.Entity<Leaf>().HasOne(x => x.LeaveType).WithMany().HasForeignKey(x => x.LeaveTypeId);
        builder.Entity<Leaf>().HasOne(x => x.LeaveState).WithMany().HasForeignKey(x => x.LeaveStateId);
        builder.Entity<LeaveRequest>().HasOne(x => x.Leave).WithMany().HasForeignKey(x => x.LeaveId);
        builder.Entity<LeaveRequest>().HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId);
        builder.Entity<Request>().HasOne(x => x.RequestState).WithMany().HasForeignKey(x => x.RequestStateId);
    }
}
