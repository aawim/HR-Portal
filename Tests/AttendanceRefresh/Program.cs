using System.Reflection;
using System.Security.Claims;
using HRM.Components.Pages.Online.Dashboard;
using HRM.DTOs.Attendance;
using HRM.DTOs.UserContext;
using HRM.Enum;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.Services.Attendance.AttendancePlan;
using HRM.Services.Attendance.Abstraction;
using HRM.Services.Attendance.Processing;
using HRM.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using Dashboard = HRM.Components.Pages.Index;
using MyAttendance = HRM.Components.Pages.Online.MyAttendanceGrid;

// Runs the real dashboard render tree and card lifecycles without a database.
// Only unrelated dashboard widgets are replaced. Clock buttons use the real
// AttendanceLogService against an isolated in-memory database. Production
// connections and employee attendance records are never used.
var failures = 0;
try { await CrudConsolidationTests.RunAsync(); Console.WriteLine("PASS CRUD consolidation: shared address creation, owner isolation, current-address handling, canonical account writes and legacy adapters"); }
catch(Exception ex) { failures++; Console.Error.WriteLine($"FAIL CRUD consolidation: {ex}"); }
try { await OrganisationResourceSettingsTests.RunAsync(); Console.WriteLine("PASS Organisation resource settings: shared types, tenant-scoped requests, calendar access, duplicate prevention and audit"); }
catch(Exception ex) { failures++; Console.Error.WriteLine($"FAIL Organisation resource settings: {ex}"); }
try { await SectionNavigationTests.RunAsync(); Console.WriteLine("PASS Section navigation: category URLs, direct links, workspace and attendance tabs, protected sections and legacy route compatibility"); }
catch(Exception ex) { failures++; Console.Error.WriteLine($"FAIL Section navigation: {ex}"); }
if(args.Contains("--navigation-only")) return failures == 0 ? 0 : 1;
try { await OrganisationPermissionTests.RunAsync(); Console.WriteLine("PASS Organisation permissions: direct/group ceilings, tenant boundaries, Super Admin management, stale edits, audit and immediate revocation"); }
catch(Exception ex) { failures++; Console.Error.WriteLine($"FAIL Organisation permissions: {ex}"); }
try { await AdminAccessGrantTests.RunAsync(); Console.WriteLine("PASS Admin access grants: scoped organisation selection, deferred roles/permissions, ordered approvals, self-approval protection, notifications, transitions, rejection, cancellation and revocation"); }
catch(Exception ex) { failures++; Console.Error.WriteLine($"FAIL Admin access grants: {ex}"); }
if(args.Contains("--admin-access-only")) return failures == 0 ? 0 : 1;
try { await AttendanceEligibilityTests.RunAsync(); Console.WriteLine("PASS Attendance eligibility: position approval date, missing/expired schedules, tenant boundaries, weekends, rotations, manual duty and no false absences"); }
catch(Exception ex) { failures++; Console.Error.WriteLine($"FAIL Attendance eligibility: {ex}"); }
try { await StaffOnboardingTests.RunAsync(); Console.WriteLine("PASS Staff onboarding: ordered approvals, deferred accounts, optional position/profile, notifications, duplicate prevention, revocation, transitions and tenant isolation"); }
catch(Exception ex) { failures++; Console.Error.WriteLine($"FAIL Staff onboarding: {ex}"); }
if(args.Contains("--onboarding-only")) return failures == 0 ? 0 : 1;
try { await StaffNotificationTests.RunAsync(); Console.WriteLine("PASS Staff notifications: business actions, bell delivery, read tracking, disabled users, ownership and tenant isolation"); }
catch(Exception ex) { failures++; Console.Error.WriteLine($"FAIL Staff notifications: {ex}"); }
try { await WorkspaceStaffDataTests.RunAsync(); Console.WriteLine("PASS Workspace staff data: staff/job mapping, selected employee attendance, clock events, employment dates, leave, weekends and tenant permissions"); }
catch(Exception ex) { failures++; Console.Error.WriteLine($"FAIL Workspace staff data: {ex}"); }
try { await LeaveEntitlementTests.RunAsync(); Console.WriteLine("PASS Leave entitlements: staff/job identity, recorded balances, effective dates, unconfirmed assignments, refresh and tenant access"); }
catch(Exception ex) { failures++; Console.Error.WriteLine($"FAIL Leave entitlements: {ex}"); }
try { await DutyRosterTests.RunAsync(); Console.WriteLine("PASS Duty roster: acceptance, supervisor approvals, transfer/swap movements, conflicts, notifications, stale states and tenant boundaries"); }
catch(Exception ex) { failures++; Console.Error.WriteLine($"FAIL Duty roster: {ex}"); }
if(args.Contains("--duty-only")) return failures == 0 ? 0 : 1;
try
{
    await WorkPlanningLifecycleTests.RunAsync();
    Console.WriteLine("PASS Work planning lifecycle: tenant-scoped templates/segments, edit persistence, snapshot refresh, historical plans, unified generation, manual duty, previews, finalization and permissions");
}
catch (Exception ex)
{
    failures++;
    Console.Error.WriteLine($"FAIL Work planning lifecycle: {ex}");
}
if (args.Contains("--schedule-only"))
{
    await ScheduleFlowTests.RunAsync();
    Console.WriteLine("PASS Schedule flow: concurrent official and duty assignments, overlapping hours, history, plan refresh, removal, permission checks and tenant isolation");
    return failures == 0 ? 0 : 1;
}
try
{
    await ScheduleFlowTests.RunAsync();
    Console.WriteLine("PASS Schedule flow: concurrent official and duty assignments, overlapping hours, plan refresh, removal and access checks");
}
catch (Exception ex)
{
    failures++;
    Console.Error.WriteLine($"FAIL Schedule flow: {ex}");
}
if (args.Contains("--accrual-only"))
{
    await LeaveAccrualTests.RunAsync();
    Console.WriteLine("PASS Leave accrual: monthly fractions, completed months, annual eligibility, political joining grants, leap dates, carry-forward, caps, tenant isolation, previews and duplicate prevention");
    return 0;
}
try
{
    await LeaveAccrualTests.RunAsync();
    Console.WriteLine("PASS Leave accrual: tenant configuration, future job types, fractional credits, eligibility, carry-forward, preview and idempotent posting");
}
catch (Exception ex)
{
    failures++;
    Console.Error.WriteLine($"FAIL Leave accrual: {ex}");
}
try
{
    await ContextSecurityTests.RunAsync();
    Console.WriteLine("PASS Context security: identity binding, sign out, disabled accounts, organisation boundaries, permission revocation and portal audit context");
}
catch (Exception ex)
{
    failures++;
    Console.Error.WriteLine($"FAIL Context security: {ex}");
}
try
{
    await AccessGroupTests.RunAsync();
    Console.WriteLine("PASS User Groups: Developer group creation, grouped permissions, audit organisation IDs, live membership, invalidation and grant limits");
}
catch (Exception ex)
{
    failures++;
    Console.Error.WriteLine($"FAIL User Groups: {ex}");
}
try
{
    await RequestTransitionTests.RunAsync();
    Console.WriteLine("PASS Request transitions: exact type/from/to validation, blocked writes, cancellation refunds, final states and Developer-only configuration");
}
catch (Exception ex)
{
    failures++;
    Console.Error.WriteLine($"FAIL Request transitions: {ex}");
}
try
{
    await WorkspaceLeaveChangesTests.RunAsync();
    Console.WriteLine("PASS Workspace leave changes: early returns, extensions, own pending requests, approval actions, refresh, deduplication and workspace wiring");
}
catch (Exception ex)
{
    failures++;
    Console.Error.WriteLine($"FAIL Workspace leave changes: {ex}");
}
try
{
    await LeaveFlowTests.RunAsync();
    Console.WriteLine("PASS Leave flows: levels, ordered User Group stages, relevant supervisors, no stage skipping, membership removal, reserved Level 0 and settings UI");
}
catch (Exception ex)
{
    failures++;
    Console.Error.WriteLine($"FAIL Leave flows: {ex}");
}
try
{
    await LeaveReceiptTests.RunAsync();
    Console.WriteLine("PASS Leave receipts: ownership, approved status, letterhead, dates, balance, reporting date, legacy data and printable page");
}
catch (Exception ex)
{
    failures++;
    Console.Error.WriteLine($"FAIL Leave receipts: {ex}");
}
try
{
    await LeaveHistoryTests.RunAsync();
    await LeaveHistoryTests.ProcessingVisibilityAsync();
    await SelfLeaveApprovalTests.RunAsync();
    Console.WriteLine("PASS Leave history: approved records, latest 10, personal ownership, status/date search and reset");
}
catch (Exception ex)
{
    failures++;
    Console.Error.WriteLine($"FAIL Leave history: {ex}");
}
try
{
    await AttendanceRequestTests.RunAsync();
    Console.WriteLine("PASS Attendance requests: date defaults, leave guards, early returns, balance refunds, corrections and duplicate protection");
}
catch (Exception ex)
{
    failures++;
    Console.Error.WriteLine($"FAIL Attendance requests: {ex}");
}
try
{
    await TestLeaveDays();
    Console.WriteLine("PASS Approved leave: overview blue markers and My Attendance leave statuses, types and totals");
}
catch (Exception ex)
{
    failures++;
    Console.Error.WriteLine($"FAIL Approved leave: {ex}");
}
foreach (var clockType in new[] { typeof(TimeClockCard), typeof(TestTimeClockCard) })
{
    try
    {
        await TestRefresh(clockType);
        Console.WriteLine($"PASS {clockType.Name}: repeated clicks save events, refresh all attendance cards, and preserve the selected overview year");
    }
    catch (Exception ex)
    {
        failures++;
        Console.Error.WriteLine($"FAIL {clockType.Name}: {ex}");
    }
}
return failures == 0 ? 0 : 1;

static async Task TestLeaveDays()
{
    var options = new DbContextOptionsBuilder<HrmTeContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
    var year = DateTime.Today.Year - 1;
    var first = new DateTime(year, 1, 1);
    await using (var db = new TestContext(options))
    {
        db.Jobs.AddRange(new Job { JobId = 1, IndividualID = 1, JobStateId = 4, JoinedDate = first.AddDays(-10) },
            new Job { JobId = 2, IndividualID = 2 },
            new Job { JobId = 3, IndividualID = 1, TerminatedDate = first.AddMonths(1) });
        db.JobPositions.Add(new JobPosition { JobPositionId = 1, JobId = 1, PositionId = 1, JobPositionStateId = 4, FromDate = first.AddDays(-10) });
        db.WorkTemplates.Add(new HRM.Models.WorkPlanning.WorkTemplate { WorkTemplateId = 1, Name = "Official schedule", IsActive = true, RequiresAttendance = true });
        db.JobWorkTemplateAssignments.Add(new HRM.Models.WorkPlanning.JobWorkTemplateAssignment { JobID = 1, WorkTemplateID = 1, IsActive = true, EffectiveFrom = first.AddDays(-10), WorkingDaysMask = 31 });
        // Persisted leave-state IDs (SharedConfig is internal to the app).
        const int approved = 4, rejected = 5, cancelled = 6, incomplete = 1;
        db.LeaveTypes.AddRange(new LeaveType { LeaveTypeId = 1, Name = "Annual Leave" },
            new LeaveType { LeaveTypeId = 2, Name = "Sick Leave" });
        db.Leaves.AddRange(
            new Leaf { JobId = 1, FromDate = first.AddDays(-2), ToDate = first.AddDays(1), LeaveStateId = approved, LeaveTypeId = 1 },
            new Leaf { JobId = 3, FromDate = first.AddDays(4).AddHours(15), ToDate = first.AddDays(5).AddHours(1), LeaveStateId = approved, LeaveTypeId = 2 },
            new Leaf { JobId = 1, FromDate = first.AddDays(5), ToDate = first.AddDays(6), LeaveStateId = approved, LeaveTypeId = 1 },
            new Leaf { JobId = 1, FromDate = first.AddDays(8), ToDate = first.AddDays(8), LeaveStateId = rejected },
            new Leaf { JobId = 1, FromDate = first.AddDays(9), ToDate = first.AddDays(9), LeaveStateId = cancelled },
            new Leaf { JobId = 1, FromDate = first.AddDays(10), ToDate = first.AddDays(10), LeaveStateId = incomplete },
            new Leaf { JobId = 2, FromDate = first.AddDays(11), ToDate = first.AddDays(11), LeaveStateId = approved });
        db.AttendanceLogs.Add(new AttendanceLog
        {
            IndividualId = 1, Date = first.AddDays(4).AddHours(10), InOutModeId = 1, AttendanceLogStateId = 1, Uidstamp = "leave-clock",
            AttendanceLogResolutions = [new AttendanceLogResolution { IsValid = true, AttendanceClockTypeId = (int)AttendanceClockType.CheckIn }]
        });
        await db.SaveChangesAsync();
    }
    var access = Proxy.For<IUserAccessService>((_, _) => Task.FromResult(new UserContextDto { IndividualId = 1 }));
    var service = new AttendanceService(new TestContextFactory(options), null!, null!, access, null!, NullLogger<AttendanceService>.Instance);
    var days = await service.GetMyYearlyAttendanceAsync(year);
    var onLeave = days.Where(x => x.IsOnLeave).ToList();
    Assert(onLeave.Count == 5, $"Expected 5 approved leave days, got {onLeave.Count}");
    Assert(onLeave.All(x => !x.IsAbsent && !x.IsPresent && !x.IsUnresolved), "Leave days counted under another status");
    Assert(days.Single(x => x.Date == first.AddDays(4)).HasAttendanceLogs, "Leave classification lost clock events");
    Assert(days.Single(x => x.Date == first.AddDays(3)).IsAbsent == !HRM.Helpers.StaffScheduleCalendar.IsWeekend(first.AddDays(3)), "Normal staff weekends counted as absent");
    Assert(days.Where(x => HRM.Helpers.StaffScheduleCalendar.IsWeekend(x.Date)).All(x => !x.IsAbsent), "Friday/Saturday inflated absent totals");

    using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
    await using var renderer = new TestRenderer(services);
    await renderer.Dispatcher.InvokeAsync(async () =>
    {
        renderer.Attach(new YearlyAttendanceGridCard());
        await renderer.Render(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Days"] = days }));
        await renderer.RootEvent("onclick", new MouseEventArgs()); // Select the previous year.
    });
    renderer.CheckErrors();
    Assert(renderer.BlueDayCount() == 5, "Approved leave dates did not render as blue day squares");

    await using var gridDb = new TestContext(options);
    using var gridServices = new ServiceCollection().AddLogging()
        .AddSingleton<IComponentActivator, TestActivator>().BuildServiceProvider();
    await using var gridRenderer = new TestRenderer(gridServices);
    var grid = new TestMyAttendance();
    var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    typeof(MyAttendance).GetProperty("DbContext", flags)!.SetValue(grid, gridDb);
    typeof(MyAttendance).GetProperty("AttendanceLogService", flags)!.SetValue(grid,
        new AttendanceLogService(gridDb, null!, null!, null!, null!));
    typeof(MyAttendance).GetProperty("_individual", flags)!.SetValue(grid,
        Proxy.For<IIndividual>((_, _) => Task.FromResult(1)));
    typeof(MyAttendance).GetField("startDate", flags)!.SetValue(grid, first);
    typeof(MyAttendance).GetField("endDate", flags)!.SetValue(grid, first.AddDays(11));
    await gridRenderer.Dispatcher.InvokeAsync(async () =>
    {
        gridRenderer.Attach(grid);
        await gridRenderer.Render();
        await grid.SearchCustomPeriod();
        typeof(MyAttendance).GetMethod("CalculateLateRecords", flags)!.Invoke(grid, null);
    });
    gridRenderer.CheckErrors();
    var rows = ((System.Collections.IEnumerable)typeof(MyAttendance).GetField("dailyRecords", flags)!.GetValue(grid)!).Cast<object>().ToList();
    string Status(object row) => (string)row.GetType().GetProperty("Status")!.GetValue(row)!;
    Assert(rows.Count(r => Status(r) == "Leave") == 5, "My Attendance did not classify all approved leave dates");
    Assert(rows.All(r => Status(r) != "Present"), "Clock events overrode approved leave");
    Assert(gridRenderer.RootText().Contains("Annual Leave") && gridRenderer.RootText().Contains("Sick Leave"), "Leave types not rendered in the table");
    Assert(gridRenderer.RootText().Contains("10:00"), "Clock time on leave was discarded");
    var late = (System.Collections.ICollection)typeof(MyAttendance).GetField("lateRecords", flags)!.GetValue(grid)!;
    Assert(late.Count == 0, "Leave days included in late records");
    var stats = ((System.Collections.IEnumerable)typeof(MyAttendance).GetField("attendanceStats", flags)!.GetValue(grid)!).Cast<object>();
    var leaveStat = stats.Single(s => (string)s.GetType().GetProperty("Label")!.GetValue(s)! == "Leaves");
    Assert((int)leaveStat.GetType().GetProperty("Days")!.GetValue(leaveStat)! == 5, "Leave chart total is incorrect");
}

static async Task TestRefresh(Type clockType)
{
    var data = new AttendanceData();
    var attendance = Proxy.For<IAttendanceService>(data.Call);
    var user = new UserContextDto
    {
        IndividualId = 1,
        ActiveJob = new ActiveJobDto { JobId = 2, OrganisationId = 3 }
    };
    var access = Proxy.For<IUserAccessService>((name, _) => name switch
    {
        "GetContextAsync" or "RequireContextAsync" => Task.FromResult(user),
        _ => throw new InvalidOperationException(name)
    });
    var plans = Proxy.For<IAttendancePlanService>((_, _) =>
        Task.FromResult<AttendanceWorkPlanDto?>(new AttendanceWorkPlanDto
        {
            WorkDate = DateTime.UtcNow.AddHours(5).Date,
            Assignments = [new() { WorkAssignmentId = 1, Name = "Official office hours", SchedulePurpose = "Official", IsValid = true,
                StartDateTime = DateTime.Today.AddHours(8), EndDateTime = DateTime.Today.AddHours(23).AddMinutes(59) },
                new() { WorkAssignmentId = 2, Name = $"Duty review {data.Revision}", SchedulePurpose = "Duty", IsValid = true,
                    StartDateTime = DateTime.Today.AddHours(10), EndDateTime = DateTime.Today.AddHours(12) }],
            Segments = [new() { Name = "Official · Check In", SchedulePurpose = "Official", StartDateTime = DateTime.Today.AddHours(8), RequiresAttendance = true, IsMandatory = true },
                new() { Name = "Official · Check Out", SchedulePurpose = "Official", StartDateTime = DateTime.Today.AddHours(14), RequiresAttendance = true, IsMandatory = true },
                new() { Name = "Duty · Check In", SchedulePurpose = "Duty", StartDateTime = DateTime.Today.AddHours(10), RequiresAttendance = true, IsMandatory = true },
                new() { Name = "Duty · Check Out", SchedulePurpose = "Duty", StartDateTime = DateTime.Today.AddHours(12), RequiresAttendance = true, IsMandatory = true }]
        }));
    await using var db = new TestContext(new DbContextOptionsBuilder<HrmTeContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    db.Jobs.Add(new Job { JobId = 2, IndividualID = 1, OrganisationID = 3 });
    await db.SaveChangesAsync();
    var processor = Proxy.For<IAttendanceLogProcessor>((_, args) => Process((int)args[0]!));
    async Task<AttendanceProcessingResult> Process(int logId)
    {
        // The display revision advances only once the actual clock row exists.
        var log = await db.AttendanceLogs.SingleAsync(x => x.AttendanceLogId == logId);
        data.Revision++;
        data.Timestamp = log.Date;
        return new AttendanceProcessingResult();
    }
    var services = new ServiceCollection()
        .AddLogging()
        .AddSingleton(attendance).AddSingleton(access).AddSingleton(plans)
        .AddSingleton(Proxy.For<IAttendanceResolver>((name, _) => throw new Exception(name)))
        .AddSingleton(new AttendanceLogService(db, processor, null!, null!, access))
        .AddSingleton<IComponentActivator, TestActivator>()
        .BuildServiceProvider();
    await using var renderer = new TestRenderer(services);
    var page = new TestDashboard();
    SetProperty(page, "AttendanceService", attendance);
    SetProperty(page, "_access", access);
    SetProperty(page, "AttendancePlanService", plans);
    SetField(page, "loading", false);
    await renderer.Dispatcher.InvokeAsync(async () =>
    {
        renderer.Attach(page);
        await renderer.Render();
    });
    renderer.CheckErrors();
    Assert(renderer.Text<TodaysAttendanceLogCard>().Contains("No clock events yet"), "Initial empty events missing");
    var workPlanText = renderer.Text<TodaysWorkPlanCard>();
    Assert(workPlanText.Contains("Today's Work Plan") && workPlanText.Contains("Official office hours") && workPlanText.Contains("Duty review 0"), "Dashboard work plan did not show both schedules");
    Assert(new[] { "08:00", "10:00", "12:00", "14:00", "Check in", "Check out", "Required" }.All(workPlanText.Contains), "Dashboard work-plan events or times missing");
    Assert(!workPlanText.Contains("23:59"), "Dashboard displayed the late clocking allowance as official working hours");
    if (clockType == typeof(TestTimeClockCard))
        await renderer.Dispatcher.InvokeAsync(() => renderer.Event(clockType, "onchange", new ChangeEventArgs { Value = true }));

    for (var revision = 1; revision <= 2; revision++)
    {
        var clock = renderer.Component(clockType);
        var callback = (EventCallback)clockType.GetProperty("OnClock")!.GetValue(clock)!;
        Assert(callback.HasDelegate, "Clock has no dashboard callback");
        await renderer.Dispatcher.InvokeAsync(() => renderer.Event(clockType, "onclick", new MouseEventArgs()));
        renderer.CheckErrors();
        Assert(await db.AttendanceLogs.CountAsync() == revision, "Clock click did not save an event");
        var time = data.Timestamp;
        Assert(renderer.Text<AttendanceSummaryCard>().Contains(time.ToString("HH:mm")), "Weekly summary still stale");
        Assert(renderer.Text<TodaysAttendanceLogCard>().Contains(time.ToString("hh:mm:ss tt")), "Clock events still stale");
        Assert(renderer.Text<TodaysAttendanceStatusCard>().Contains(time.ToString("hh:mm tt")), "Daily status still stale");
        var overview = (YearlyAttendanceGridCard)renderer.Component(typeof(YearlyAttendanceGridCard));
        Assert(overview.Days?.Single().ClockEventCount == revision, "Yearly overview still stale");
        Assert(data.LogReads == revision + 1, "Clock events did not reload exactly once per click");
        Assert(data.EvaluationReads == revision + 1, "Daily status did not reload exactly once per click");
        Assert(renderer.Text<TodaysWorkPlanCard>().Contains($"Duty review {revision}"), "Today's Work Plan did not refresh after clocking");
    }
    await renderer.Dispatcher.InvokeAsync(() => renderer.Event(typeof(YearlyAttendanceGridCard), "onclick", new MouseEventArgs()));
    var selectedYear = DateTime.Today.Year - 1;
    Assert(data.LastYear == selectedYear, "Overview year navigation failed");
    await renderer.Dispatcher.InvokeAsync(() => renderer.Event(clockType, "onclick", new MouseEventArgs()));
    renderer.CheckErrors();
    Assert(data.LastYear == selectedYear, "Clock click reset the selected overview year");
    Assert(await db.AttendanceLogs.CountAsync() == 3, "Third event was not saved");
    Assert(data.LogReads == 4 && data.EvaluationReads == 4, "Third click did not refresh the daily cards");
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
static void SetProperty(object target, string name, object value) =>
    typeof(Dashboard).GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(target, value);
static void SetField(object target, string name, object value) =>
    typeof(Dashboard).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);

sealed class AttendanceData
{
    public int Revision;
    public int LogReads;
    public int EvaluationReads;
    public int LastYear;
    public DateTime Timestamp { get; set; } = DateTime.Today;
    private AttendanceLogDto Log => new() { Date = Timestamp, ResolvedClockType = AttendanceClockType.CheckIn };
    public object Call(string name, object?[] args) => name switch
    {
        "GetMyWeeklyAttendanceAsync" => Yield(Revision == 0 ? new List<AttendanceLogDto>() : new List<AttendanceLogDto> { Log }),
        "GetTodayStatusAsync" => Yield<(AttendanceLogDto?, AttendanceLogDto?)>((Revision == 0 ? null : Log, null)),
        "GetMyYearlyAttendanceAsync" => Yearly((int)args[0]!),
        "GetLogsAsync" => Logs(),
        "GetEvaluationAsync" => Evaluation(),
        _ => throw new InvalidOperationException($"Unexpected attendance call: {name}")
    };
    private Task<List<YearlyAttendanceDayDto>> Yearly(int year)
    {
        LastYear = year;
        return Yield(new List<YearlyAttendanceDayDto>
        {
            new() { Date = DateTime.Today, IsPresent = Revision > 0, HasAttendanceLogs = Revision > 0, ClockEventCount = Revision }
        });
    }
    private static async Task<T> Yield<T>(T result)
    {
        await Task.Yield();
        return result;
    }
    private Task<List<AttendanceRawLogDto>> Logs()
    {
        LogReads++;
        return Yield(Revision == 0 ? new List<AttendanceRawLogDto>() :
            new List<AttendanceRawLogDto> { new() { AttendanceLogId = Revision, LogDateTime = Timestamp } });
    }
    private Task<AttendanceDailyEvaluationDto> Evaluation()
    {
        EvaluationReads++;
        return Yield(new AttendanceDailyEvaluationDto
        {
            FirstCheckIn = Revision == 0 ? null : Timestamp,
            RequiresAttendance = true, WorkDate = DateTime.Today
        });
    }
}

public class Proxy : DispatchProxy
{
    private Func<string, object?[], object?> handler = null!;
    public static T For<T>(Func<string, object?[], object?> handler) where T : class
    {
        var proxy = Create<T, Proxy>();
        ((Proxy)(object)proxy).handler = handler;
        return proxy;
    }
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => handler(targetMethod!.Name, args ?? []);
}

sealed class TestDashboard : Dashboard
{
    protected override Task OnInitializedAsync() => Task.CompletedTask;
}

sealed class TestMyAttendance : MyAttendance
{
    protected override Task OnInitializedAsync() => Task.CompletedTask;
    protected override Task OnParametersSetAsync() => Task.CompletedTask;
}

sealed class TestActivator : IComponentActivator
{
    private static readonly HashSet<Type> RealComponents =
    [typeof(TimeClockCard), typeof(TestTimeClockCard), typeof(AttendanceSummaryCard),
     typeof(YearlyAttendanceGridCard), typeof(TodaysAttendanceLogCard), typeof(TodaysAttendanceStatusCard), typeof(TodaysWorkPlanCard)];
    public IComponent CreateInstance(Type type) => type == typeof(AuthorizeView)
        ? new AuthorizedView()
        : RealComponents.Contains(type) ? (IComponent)Activator.CreateInstance(type)! : new EmptyWidget();
}

sealed class EmptyWidget : IComponent
{
    public void Attach(RenderHandle renderHandle) { }
    public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
}

sealed class AuthorizedView : ComponentBase
{
    private RenderFragment<AuthenticationState>? authorized;
    public override Task SetParametersAsync(ParameterView parameters)
    {
        authorized = parameters.GetValueOrDefault<RenderFragment<AuthenticationState>>("Authorized");
        StateHasChanged();
        return Task.CompletedTask;
    }
    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0,
        authorized?.Invoke(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity([], "test")))));
}

sealed class TestRenderer : Renderer
{
    private int root;
    private readonly List<Exception> errors = [];
    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();
    public TestRenderer(IServiceProvider services) : base(services, services.GetRequiredService<ILoggerFactory>()) { }
    protected override IComponent ResolveComponentForRenderMode(Type componentType, int? parentComponentId,
        IComponentActivator componentActivator, IComponentRenderMode renderMode) => componentActivator.CreateInstance(componentType);
    public void Attach(IComponent component) => root = AssignRootComponentId(component);
    public Task Render() => RenderRootComponentAsync(root);
    public Task Render(ParameterView parameters) => RenderRootComponentAsync(root, parameters);
    public Task RootEvent(string attribute, EventArgs args) => EventFor(root, attribute, args);
    public Task RootClick(string label)
    {
        var frames = GetCurrentRenderTreeFrames(root);
        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];
            if (frame.FrameType != RenderTreeFrameType.Element || frame.ElementName != "button") continue;
            var contents = frames.Array.Skip(i + 1).Take(frame.ElementSubtreeLength - 1).ToArray();
            if (!contents.Any(x =>
                (x.FrameType == RenderTreeFrameType.Text && x.TextContent.Contains(label, StringComparison.Ordinal)) ||
                (x.FrameType == RenderTreeFrameType.Markup && x.MarkupContent.Contains(label, StringComparison.Ordinal)))) continue;
            var handler = contents.First(x => x.FrameType == RenderTreeFrameType.Attribute && x.AttributeName == "onclick");
            return DispatchEventAsync(handler.AttributeEventHandlerId, null, new MouseEventArgs(), waitForQuiescence: true);
        }
        throw new Exception($"Button not found: {label}");
    }
    public int RootButtonCount(string label)
    {
        var frames = GetCurrentRenderTreeFrames(root);
        var count = 0;
        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];
            if (frame.FrameType != RenderTreeFrameType.Element || frame.ElementName != "button") continue;
            if (frames.Array.Skip(i + 1).Take(frame.ElementSubtreeLength - 1).Any(x =>
                (x.FrameType == RenderTreeFrameType.Text && x.TextContent.Contains(label, StringComparison.Ordinal)) ||
                (x.FrameType == RenderTreeFrameType.Markup && x.MarkupContent.Contains(label, StringComparison.Ordinal)))) count++;
        }
        return count;
    }
    public Task RootSelect(string value)
    {
        var frames = GetCurrentRenderTreeFrames(root);
        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];
            if (frame.FrameType != RenderTreeFrameType.Element || frame.ElementName != "select") continue;
            var contents = frames.Array.Skip(i + 1).Take(frame.ElementSubtreeLength - 1).ToArray();
            if (!contents.Any(x => x.FrameType == RenderTreeFrameType.Attribute && x.AttributeName == "value" && x.AttributeValue?.ToString() == value)) continue;
            var handler = contents.FirstOrDefault(x => x.FrameType == RenderTreeFrameType.Attribute && x.AttributeName == "onchange");
            if (handler.AttributeEventHandlerId == 0) continue;
            return DispatchEventAsync(handler.AttributeEventHandlerId, null, new ChangeEventArgs { Value = value }, waitForQuiescence: true);
        }
        throw new Exception($"Select option not found: {value}");
    }
    public Task RootChange(string elementId, string value)
    {
        var frames = GetCurrentRenderTreeFrames(root);
        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];
            if (frame.FrameType != RenderTreeFrameType.Element) continue;
            var contents = frames.Array.Skip(i + 1).TakeWhile(x => x.FrameType == RenderTreeFrameType.Attribute).ToArray();
            if (!contents.Any(x => x.AttributeName == "id" && x.AttributeValue?.ToString() == elementId)) continue;
            var handler = contents.First(x => x.AttributeName == "onchange");
            return DispatchEventAsync(handler.AttributeEventHandlerId, null, new ChangeEventArgs { Value = value }, waitForQuiescence: true);
        }
        throw new Exception($"Input not found: {elementId}");
    }
    public List<string> RootValues()
    {
        var frames = GetCurrentRenderTreeFrames(root);
        return frames.Array.Take(frames.Count).Where(x => x.FrameType == RenderTreeFrameType.Attribute && x.AttributeName == "value")
            .Select(x => x.AttributeValue?.ToString() ?? "").ToList();
    }
    public int RootDateInputCount()
    {
        var frames = GetCurrentRenderTreeFrames(root);
        var count = 0;
        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Element && frame.ElementName == "input" &&
                frames.Array.Skip(i + 1).Take(frame.ElementSubtreeLength - 1).Any(x =>
                    x.FrameType == RenderTreeFrameType.Attribute && x.AttributeName == "type" && Equals(x.AttributeValue, "date"))) count++;
        }
        return count;
    }
    public int BlueDayCount()
    {
        var frames = GetCurrentRenderTreeFrames(root);
        return frames.Array.Take(frames.Count).Count(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class" &&
            f.AttributeValue is string css && css.Contains("h-full w-full") && css.Contains("bg-blue-400"));
    }
    public int RedWeeklyDayCount()
    {
        var frames = GetCurrentRenderTreeFrames(root);
        return frames.Array.Take(frames.Count).Count(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class" &&
            f.AttributeValue is string css && css.Contains("h-28") && css.Contains("bg-red-500"));
    }
    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
    protected override void HandleException(Exception exception) => errors.Add(exception);
    public void CheckErrors()
    {
        if (errors.Count > 0) throw new AggregateException(errors);
    }
    private IEnumerable<(int Id, IComponent Component)> Children(int id)
    {
        var frames = GetCurrentRenderTreeFrames(id);
        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];
            if (frame.FrameType != RenderTreeFrameType.Component) continue;
            yield return (frame.ComponentId, frame.Component);
            foreach (var child in Children(frame.ComponentId)) yield return child;
        }
    }
    public IComponent Component(Type type) => Children(root).Single(x => x.Component.GetType() == type).Component;
    public Task Event(Type type, string attribute, EventArgs args)
    {
        var id = Children(root).Single(x => x.Component.GetType() == type).Id;
        return EventFor(id, attribute, args);
    }
    private Task EventFor(int id, string attribute, EventArgs args)
    {
        var frames = GetCurrentRenderTreeFrames(id);
        var handler = frames.Array.Take(frames.Count).First(x =>
            x.FrameType == RenderTreeFrameType.Attribute && x.AttributeName == attribute);
        return DispatchEventAsync(handler.AttributeEventHandlerId, null, args, waitForQuiescence: true);
    }
    public string Text<T>()
    {
        var id = Children(root).Single(x => x.Component is T).Id;
        return TextFor(id);
    }
    public string RootText() => TextFor(root);
    private string TextFor(int id)
    {
        var frames = GetCurrentRenderTreeFrames(id);
        return string.Join(" ", frames.Array.Take(frames.Count).Select(f => f.FrameType switch
        {
            RenderTreeFrameType.Text => f.TextContent,
            RenderTreeFrameType.Markup => f.MarkupContent,
            _ => ""
        }));
    }
}

sealed class TestContext(DbContextOptions<HrmTeContext> options) : HrmTeContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        var types = new[] { typeof(Job), typeof(AttendanceLog), typeof(Leaf), typeof(LeaveType), typeof(AttendanceLogResolution),
            typeof(HRM.Models.WorkPlanning.JobWorkTemplateAssignment), typeof(JobWorkTemplate), typeof(HRM.Models.WorkPlanning.WorkPlan), typeof(HRM.Models.WorkPlanning.WorkAssignment) }.Concat(ScheduleTestModel.EligibilityTypes).Distinct().ToArray();
        foreach (var type in builder.Model.GetEntityTypes().Select(x => x.ClrType).ToArray())
            if (!types.Contains(type)) builder.Ignore(type);
        foreach (var type in types)
            foreach (var property in type.GetProperties().Where(x =>
                !x.PropertyType.IsValueType && x.PropertyType != typeof(string) && x.PropertyType != typeof(byte[])))
                if (!(type == typeof(Leaf) && (property.Name == "Job" || property.Name == "LeaveType")) &&
                    !(type == typeof(AttendanceLog) && property.Name == "AttendanceLogResolutions") &&
                    !(type == typeof(AttendanceLogResolution) && property.Name == "AttendanceLog") &&
                    !ScheduleTestModel.Navigation(type, property.Name))
                    builder.Entity(type).Ignore(property.Name);
        builder.Entity<Job>().HasKey(x => x.JobId);
        builder.Entity<AttendanceLog>().HasKey(x => x.AttendanceLogId);
        builder.Entity<Leaf>().HasKey(x => x.LeaveId);
        builder.Entity<Leaf>().HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId);
        builder.Entity<LeaveType>().HasKey(x => x.LeaveTypeId);
        builder.Entity<Leaf>().HasOne(x => x.LeaveType).WithMany().HasForeignKey(x => x.LeaveTypeId);
        builder.Entity<AttendanceLogResolution>().HasKey(x => x.AttendanceLogResolutionId);
        builder.Entity<AttendanceLogResolution>().HasOne(x => x.AttendanceLog).WithMany(x => x.AttendanceLogResolutions).HasForeignKey(x => x.AttendanceLogId);
        ScheduleTestModel.Configure(builder);
    }
}

sealed class TestContextFactory(DbContextOptions<HrmTeContext> options) : IDbContextFactory<HrmTeContext>
{
    public HrmTeContext CreateDbContext() => new TestContext(options);
}
