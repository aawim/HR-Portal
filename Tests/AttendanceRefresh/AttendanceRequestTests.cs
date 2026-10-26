using System.Reflection;
using HRM.DTOs;
using HRM.DTOs.Attendance;
using HRM.DTOs.Leave;
using HRM.DTOs.UserContext;
using HRM.Helpers;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.LeaveTypes;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using HRM.Services.Interfaces.Leave;
using HRM.Services.Leave;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using RequestPage = HRM.Components.Pages.Online.AttendanceRequestGrid;

static class AttendanceRequestTests
{
    public static async Task RunAsync()
    {
        var today = DateTime.UtcNow.AddHours(5).Date;
        Check(new AttendanceRequestApplicationDto().Date == DateOnly.FromDateTime(today), "New panel date is not today");
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<HrmTeContext>().UseSqlite(connection).Options;
        var factory = new RequestTestFactory(options);
        await using (var db = factory.CreateDbContext())
        {
            await db.Database.EnsureCreatedAsync();
            db.Jobs.AddRange(new Job { JobId = 1, IndividualID = 1, OrganisationID = 10 },
                new Job { JobId = 2, IndividualID = 2, OrganisationID = 10 });
            db.LeaveTypes.Add(new LeaveType { LeaveTypeId = 1, Name = "Annual Leave", IncludeHolidays = true });
            db.Leaves.AddRange(
                new Leaf { LeaveId = 1, JobId = 1, LeaveTypeId = 1, FromDate = today.AddDays(-5), ToDate = today.AddDays(-1), Duration = 5, LeaveStateId = 4 },
                new Leaf { LeaveId = 2, JobId = 1, LeaveTypeId = 1, FromDate = today, ToDate = today, Duration = 1, LeaveStateId = 4 },
                new Leaf { LeaveId = 3, JobId = 1, LeaveTypeId = 1, FromDate = today.AddDays(2), ToDate = today.AddDays(6), Duration = 5, LeaveStateId = 4 },
                new Leaf { LeaveId = 4, JobId = 1, LeaveTypeId = 1, FromDate = today.AddDays(20), ToDate = today.AddDays(22), Duration = 3, LeaveStateId = 4 });
            db.JobLeaveTypes.Add(new JobLeaveType { JobLeaveTypeId = 1, JobId = 1, LeaveTypeId = 1, RemainingDays = 10, IsValid = true });
            db.RequestStates.AddRange(new RequestState { RequestStateId = 2, StateName = "Pending verification", IsProcessingState = true },
                new RequestState { RequestStateId = 4, StateName = "Approved", IsFinalState = true });
            db.AttendanceLogs.AddRange(Clock(1, 1, today.AddDays(-1)), Clock(2, 1, today.AddDays(-8)), Clock(3, 2, today.AddDays(-8)));
            await db.SaveChangesAsync();
        }
        var user = new UserContextDto { IndividualId = 1, BusinessEntityId = 1, UserId = 1, ActiveJob = new ActiveJobDto { JobId = 1, OrganisationId = 10 } };
        var access = Proxy.For<IUserAccessService>((_, _) => Task.FromResult(user));
        var requestWriter = Proxy.For<IRequestService>((_, args) => WriteRequest((HrmTeContext)args[0]!, (CreateRequestDto)args[1]!));
        var logNumber = 100;
        var audit = Proxy.For<IOperationLogService>((_, _) => Task.FromResult(new OperationLog { OperationLogId = ++logNumber }));
        var workflowCalls = 0;
        var failWorkflow = false;
        var workflow = Proxy.For<IWorkflowEngine>((_, _) =>
        {
            if (failWorkflow) throw new InvalidOperationException("Test workflow unavailable");
            workflowCalls++;
            return Task.FromResult((long)workflowCalls);
        });
        var leaveChanges = new LeaveChangeService(factory, access, requestWriter, audit, workflow, null!, null!);
        var attendance = Proxy.For<IAttendanceService>((_, _) => Task.FromResult(new List<AttendanceLogDto>
            { new() { AttendanceLogID = 1 }, new() { AttendanceLogID = 2 }, new() { AttendanceLogID = 3 }, new() { AttendanceLogID = 10 }, new() { AttendanceLogID = 14 } }));
        var service = new AttendanceRequestService(factory, access, attendance, leaveChanges, requestWriter, audit, workflow);
        await TestPanel(service, today);
        Check(!(await service.GetDateInfoAsync(DateOnly.FromDateTime(today.AddDays(-1)))).CanSubmit, "Past leave date was allowed");
        Check(!(await service.GetDateInfoAsync(DateOnly.FromDateTime(today))).CanSubmit, "Current leave date was allowed");
        Check(!(await service.GetDateInfoAsync(default)).CanSubmit, "Year 0001 was allowed");
        var future = await service.GetDateInfoAsync(DateOnly.FromDateTime(today.AddDays(4)));
        Check(future.IsEarlyReturn && future.BalanceAdjustment == 3, "Future leave did not preview a three-day refund");
        var full = await service.GetDateInfoAsync(DateOnly.FromDateTime(today.AddDays(2)));
        Check(full.IsEarlyReturn && full.BalanceAdjustment == 5, "First-day return did not preview a full refund");
        Check(!(await service.SubmitAsync(Application(today.AddDays(-1)))).Success, "Past leave attendance was saved");
        Check(!(await service.SubmitAsync(Application(today.AddDays(-9), original: 1))).Success, "Original leave date was bypassed by moving the correction");
        Check(!(await service.SubmitAsync(Application(today.AddDays(-8), original: 3))).Success, "Another employee's event was accepted");
        Check(!(await service.SubmitAsync(Application(today.AddDays(4), original: 2))).Success, "Correction was moved onto approved future leave");
        Check(!(await service.SubmitAsync(Application(today.AddDays(-9), original: 2))).Success, "Correction was allowed to change the clock event date");

        var returned = await service.SubmitAsync(Application(today.AddDays(4)));
        Check(returned.Success, returned.Message);
        await using (var db = factory.CreateDbContext())
            Check(await db.LeaveChangeRequests.AnyAsync(x => x.Request.RequestTypeId == 105), "Early return was not submitted as its own Request Type");
        await using (var db = factory.CreateDbContext())
        {
            Check(await db.AttendanceLogs.CountAsync() == 3, "Early return created a future clock event");
            Check((await db.JobLeaveTypes.SingleAsync()).RemainingDays == 10, "Balance changed before approval");
            Check((await db.Leaves.SingleAsync(x => x.LeaveId == 3)).ToDate == today.AddDays(6), "Leave was shortened before approval");
        }
        Check(!(await service.SubmitAsync(Application(today.AddDays(4)))).Success, "Duplicate early return was accepted");
        await ApproveChange(factory, leaveChanges, 13, today.AddDays(3), false);
        Check((await service.SubmitAsync(Application(today.AddDays(2)))).Success, "Full remaining-leave return was not submitted");
        await ApproveChange(factory, leaveChanges, 15, today.AddDays(3), true);
        var extension = await leaveChanges.SubmitAsync(new LeaveChangeApplicationDto
            { LeaveId = 4, Kind = LeaveChangeKind.Extension, Date = today.AddDays(24), Reason = "Extend approved leave" });
        Check(extension.Success, extension.Message);
        await using (var db = factory.CreateDbContext())
            Check(await db.LeaveChangeRequests.AnyAsync(x => x.Request.RequestTypeId == 106), "Extension was not submitted as its own Request Type");
        await ApproveChange(factory, leaveChanges, 13, today.AddDays(24), false);
        var excessiveExtension = await leaveChanges.SubmitAsync(new LeaveChangeApplicationDto
            { LeaveId = 4, Kind = LeaveChangeKind.Extension, Date = today.AddDays(200), Reason = "Insufficient balance test" });
        Check(!excessiveExtension.Success, "Extension exceeding the remaining leave balance was accepted");

        var missing = Application(today.AddDays(-9));
        var saved = await service.SubmitAsync(missing);
        Check(saved.Success, saved.Message);
        Check(!(await service.SubmitAsync(missing)).Success, "Duplicate attendance request was accepted");
        var correction = Application(today.AddDays(-8), original: 2);
        var corrected = await service.SubmitAsync(correction);
        Check(corrected.Success, corrected.Message);
        Check(!(await service.SubmitAsync(correction)).Success, "Duplicate correction was accepted");
        int correctionRequest;
        await using (var db = factory.CreateDbContext())
        {
            var original = await db.AttendanceLogs.SingleAsync(x => x.AttendanceLogId == 2);
            Check(original.Date == today.AddDays(-8).AddHours(10) && original.AttendanceLogStateId == 1, "Submission overwrote the original event");
            var proposed = await db.AttendanceLogs.SingleAsync(x => x.RelatedAttendanceLogId == 2);
            Check(proposed.AttendanceLogStateId == 2 && proposed.Date.Hour == 8, "Correction was not stored as a pending proposed event");
            correctionRequest = (await db.AttendanceLogChangeRequests.SingleAsync()).RequestId;
        }
        var resource = await new AttendanceRequestResourceBuilder(factory).BuildAsync(correctionRequest);
        Check(resource?.IndividualID == 1 && resource.AttendanceLogStateID == 2, "Correction request resource was not available");
        var realAttendance = new AttendanceService(factory, null!, null!, access, null!, NullLogger<AttendanceService>.Instance);
        var yearDays = await realAttendance.GetMyYearlyAttendanceAsync(today.AddDays(-9).Year);
        Check(!yearDays.Single(x => x.Date == today.AddDays(-9)).HasAttendanceLogs, "Pending request polluted attendance overview");
        await TestPastLeaveCheckIn(factory, service, today);

        int beforeRequests, beforeLogs;
        await using (var db = factory.CreateDbContext()) { beforeRequests = await db.Requests.CountAsync(); beforeLogs = await db.AttendanceLogs.CountAsync(); }
        failWorkflow = true;
        Check(!(await service.SubmitAsync(Application(today.AddDays(-10)))).Success, "Unavailable workflow did not fail submission");
        await using (var db = factory.CreateDbContext())
        {
            Check(await db.Requests.CountAsync() == beforeRequests && await db.AttendanceLogs.CountAsync() == beforeLogs,
                "Failed workflow left a partial request or proposed event behind");
        }
        var weekdayRefund = LeaveChangeRules.Calculate(new DateTime(2026, 10, 18), new DateTime(2026, 10, 24), 5,
            LeaveChangeKind.EarlyReturn, new DateTime(2026, 10, 22), false);
        Check(weekdayRefund.BalanceAdjustment == 1, "Refund included nonchargeable Friday/Saturday days");
    }

    private static async Task TestPastLeaveCheckIn(RequestTestFactory factory, AttendanceRequestService service, DateTime today)
    {
        int leaveChangesBefore;
        decimal? balanceBefore;
        await using (var db = factory.CreateDbContext())
        {
            var checkout = Clock(10, 1, today.AddDays(-2));
            checkout.InOutModeId = 2;
            var otherEmployeeCheckIn = Clock(11, 2, today.AddDays(-2));
            var pendingCheckIn = Clock(12, 1, today.AddDays(-2));
            pendingCheckIn.AttendanceLogStateId = 2;
            var automaticCheckIn = Clock(13, 1, today.AddDays(-3));
            automaticCheckIn.InOutModeId = 3;
            automaticCheckIn.AttendanceLogResolutions.Add(new AttendanceLogResolution { AttendanceClockTypeId = 1, IsValid = true });
            var unevenCheckout = Clock(14, 1, today.AddDays(-3));
            unevenCheckout.InOutModeId = 2;
            db.AttendanceLogs.AddRange(checkout, otherEmployeeCheckIn, pendingCheckIn, automaticCheckIn, unevenCheckout);
            await db.SaveChangesAsync();
            leaveChangesBefore = await db.LeaveChangeRequests.CountAsync();
            balanceBefore = (await db.JobLeaveTypes.SingleAsync()).RemainingDays;
        }
        Check(!(await service.GetDateInfoAsync(DateOnly.FromDateTime(today.AddDays(-2)), 10)).CanSubmit,
            "Checkout, another employee's check-in or pending check-in unlocked correction on leave");
        Check(!(await service.SubmitAsync(Application(today.AddDays(-2), 10))).Success,
            "Past leave without the employee's check-in allowed a correction");
        await TestCorrectionActions(service, today);
        Check((await service.GetDateInfoAsync(DateOnly.FromDateTime(today.AddDays(-1)), 1)).CanSubmit,
            "An existing legacy check-in did not allow correction on past leave");
        var legacyCorrection = await service.SubmitAsync(Application(today.AddDays(-1), 1));
        Check(legacyCorrection.Success, legacyCorrection.Message);
        Check((await service.GetDateInfoAsync(DateOnly.FromDateTime(today.AddDays(-3)), 14)).CanSubmit,
            "A resolved automatic check-in did not allow another uneven event on the same date to be corrected");
        var automaticCorrection = await service.SubmitAsync(Application(today.AddDays(-3), 14));
        Check(automaticCorrection.Success, automaticCorrection.Message);
        Check(!(await service.SubmitAsync(Application(today.AddDays(-3)))).Success,
            "Check-in exception incorrectly allowed a new missing-attendance request on leave");
        await using (var db = factory.CreateDbContext())
        {
            Check((await db.JobLeaveTypes.SingleAsync()).RemainingDays == balanceBefore &&
                await db.LeaveChangeRequests.CountAsync() == leaveChangesBefore,
                "A correction on past leave changed the leave balance or created an early return");
            Check((await db.Leaves.SingleAsync(x => x.LeaveId == 1)).LeaveStateId == 4,
                "A correction changed the original approved leave");
            db.AttendanceLogResolutions.Add(new AttendanceLogResolution { AttendanceLogId = 13, AttendanceClockTypeId = 2, IsValid = true });
            await db.SaveChangesAsync();
        }
        Check(!(await service.GetDateInfoAsync(DateOnly.FromDateTime(today.AddDays(-3)), 14)).CanSubmit,
            "A superseded check-in resolution still unlocked leave-date correction");
    }

    private static async Task TestCorrectionActions(AttendanceRequestService service, DateTime today)
    {
        using var services = new ServiceCollection().AddLogging().AddSingleton<IComponentActivator, TestActivator>().BuildServiceProvider();
        await using var renderer = new TestRenderer(services);
        var page = new RequestTestPage();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        typeof(RequestPage).GetProperty("AttendanceRequests", flags)!.SetValue(page, service);
        typeof(RequestPage).GetField("unevenLogs", flags)!.SetValue(page, new List<AttendanceLogDto>
        {
            new() { AttendanceLogID = 10, Date = today.AddDays(-2).AddHours(10) },
            new() { AttendanceLogID = 14, Date = today.AddDays(-3).AddHours(10) },
            new() { AttendanceLogID = 1, Date = today.AddDays(-1).AddHours(10) }
        });
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await (Task)typeof(RequestPage).GetMethod("LoadCorrectionEligibility", flags)!.Invoke(page, null)!;
            renderer.Attach(page);
            await renderer.Render();
        });
        renderer.CheckErrors();
        await renderer.Dispatcher.InvokeAsync(() => renderer.Render(ParameterView.FromDictionary(
            new Dictionary<string, object?> { ["TabSection"] = "uneven-log-correction" })));
        renderer.CheckErrors();
        Check(renderer.RootButtonCount("Request correction") == 2, "Blocked leave-date correction button is still visible, or eligible buttons were hidden");
        await renderer.Dispatcher.InvokeAsync(() => renderer.RootClick("Request correction"));
        renderer.CheckErrors();
        var model = (AttendanceRequestApplicationDto)typeof(RequestPage).GetField("newRequest", flags)!.GetValue(page)!;
        Check(model.OriginalLogId == 14, "First correction button did not belong to an eligible event");
        Check(!renderer.RootValues().Contains("10") && renderer.RootValues().Contains("14") && renderer.RootValues().Contains("1"),
            "Correction dropdown still offers blocked leave dates, or omits eligible check-in dates");
    }

    private static async Task TestPanel(AttendanceRequestService service, DateTime today)
    {
        using var services = new ServiceCollection().AddLogging().AddSingleton<IComponentActivator, TestActivator>().BuildServiceProvider();
        await using var renderer = new TestRenderer(services);
        var page = new RequestTestPage();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        typeof(RequestPage).GetProperty("AttendanceRequests", flags)!.SetValue(page, service);
        var submitted = new List<LeaveChangeApplicationDto>();
        var choices = new List<LeaveChangeLeaveDto>
        {
            new() { LeaveId = 3, LeaveType = "Annual Leave", FromDate = today.AddDays(2), ToDate = today.AddDays(6), Duration = 5, IncludeHolidays = true },
            new() { LeaveId = 99, LeaveType = "Sick Leave", FromDate = today.AddDays(10), ToDate = today.AddDays(11), Duration = 2, HasPendingChange = true }
        };
        var leaveService = Proxy.For<ILeaveChangeService>((name, args) => name switch
        {
            "GetMyLeavesAsync" => Task.FromResult(choices),
            "SubmitAsync" => Capture((LeaveChangeApplicationDto)args[0]!),
            _ => throw new Exception(name)
        });
        Task<ServiceResult> Capture(LeaveChangeApplicationDto application)
        {
            submitted.Add(application);
            return Task.FromResult(ServiceResult.Ok("Leave change submitted for approval."));
        }
        typeof(RequestPage).GetProperty("LeaveChanges", flags)!.SetValue(page, leaveService);
        typeof(RequestPage).GetField("startDate", flags)!.SetValue(page, today.AddDays(-10));
        typeof(RequestPage).GetField("endDate", flags)!.SetValue(page, today.AddDays(10));
        typeof(RequestPage).GetField("unevenLogs", flags)!.SetValue(page,
            new List<AttendanceLogDto> { new() { AttendanceLogID = 2, Date = today.AddDays(-8).AddHours(10) },
                new() { AttendanceLogID = 1, Date = today.AddDays(-1).AddHours(10) } });
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await (Task)typeof(RequestPage).GetMethod("LoadCorrectionEligibility", flags)!.Invoke(page, null)!;
            renderer.Attach(page);
            await renderer.Render();
            await renderer.RootClick("Add Request");
        });
        renderer.CheckErrors();
        Check(renderer.RootValues().Contains(today.ToString("yyyy-MM-dd")) && !renderer.RootValues().Contains("0001-01-01"), "Rendered calendar does not default to today");
        Check(renderer.RootDateInputCount() == 3, "Missing attendance should retain its date picker");
        Check(renderer.RootText().Contains("Attendance cannot be requested"), "Panel did not explain the current leave restriction");
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await renderer.RootClick("Cancel");
            await renderer.Render(ParameterView.FromDictionary(new Dictionary<string, object?> { ["TabSection"] = "uneven-log-correction" }));
            await renderer.RootClick("Request correction");
        });
        renderer.CheckErrors();
        var model = (AttendanceRequestApplicationDto)typeof(RequestPage).GetField("newRequest", flags)!.GetValue(page)!;
        Check(model.OriginalLogId == 2 && model.Date == DateOnly.FromDateTime(today.AddDays(-8)), "Correction button did not prefill the event");
        Check(renderer.RootDateInputCount() == 2, "Uneven-log correction still asks the user to select a date");
        await renderer.Dispatcher.InvokeAsync(() => renderer.RootSelect("1"));
        renderer.CheckErrors();
        var dateInfo = (AttendanceRequestDateInfoDto)typeof(RequestPage).GetField("requestDateInfo", flags)!.GetValue(page)!;
        Check(dateInfo.CanSubmit && renderer.RootText().Contains("has a check-in event"),
            "Correction form did not allow a past leave date with an existing check-in");
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await renderer.RootClick("Cancel");
            await renderer.Render(ParameterView.FromDictionary(new Dictionary<string, object?> { ["TabSection"] = "attendance" }));
            await renderer.RootClick("Add Request");
            model = (AttendanceRequestApplicationDto)typeof(RequestPage).GetField("newRequest", flags)!.GetValue(page)!;
            model.Date = DateOnly.FromDateTime(today.AddDays(4));
            await (Task)typeof(RequestPage).GetMethod("RefreshDateInfo", flags)!.Invoke(page, null)!;
            await renderer.Render();
        });
        renderer.CheckErrors();
        Check(renderer.RootText().Contains("Request early return") && renderer.RootText().Contains("3 unused leave days"), "Future leave panel did not preview the early return and refund");
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await renderer.RootSelect("leave");
            await renderer.RootSelect("3");
        });
        renderer.CheckErrors();
        Check(renderer.RootText().Contains("Approved leave") && renderer.RootText().Contains("Annual Leave"), "Approved leave selection is missing");
        Check(renderer.RootText().Contains("Request extension") && renderer.RootText().Contains("1 additional leave days"), "Extension preview is incorrect");
        Check(!renderer.RootText().Contains("In/Out Mode"), "Leave change form asks for a clock mode");
        model.Comments = "Extend selected leave";
        await renderer.Dispatcher.InvokeAsync(() => renderer.RootClick("Request extension"));
        renderer.CheckErrors();
        Check(submitted.Count == 1 && submitted[0].LeaveId == 3 && submitted[0].Kind == LeaveChangeKind.Extension && submitted[0].Date == today.AddDays(7), "Selected leave extension was not submitted");
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await renderer.RootClick("Add Request");
            await renderer.RootSelect("leave");
            await renderer.RootSelect("3");
            await renderer.RootSelect("EarlyReturn");
            var change = (LeaveChangeApplicationDto)typeof(RequestPage).GetField("leaveChange", flags)!.GetValue(page)!;
            change.Date = today.AddDays(4);
            model = (AttendanceRequestApplicationDto)typeof(RequestPage).GetField("newRequest", flags)!.GetValue(page)!;
            model.Comments = "Return to work early";
            await renderer.Render();
        });
        renderer.CheckErrors();
        Check(renderer.RootText().Contains("3 unused leave days"), "Selected leave early-return preview is incorrect");
        await renderer.Dispatcher.InvokeAsync(() => renderer.RootClick("Request early return"));
        renderer.CheckErrors();
        Check(submitted.Count == 2 && submitted[1].Kind == LeaveChangeKind.EarlyReturn && submitted[1].Date == today.AddDays(4) && submitted[1].Reason == "Return to work early", "Selected leave early return was not submitted");
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await renderer.RootClick("Add Request");
            await renderer.RootSelect("leave");
            await renderer.RootSelect("99"); // Simulate tampering with a disabled pending leave option.
            await renderer.RootClick("Request extension");
        });
        renderer.CheckErrors();
        Check(submitted.Count == 2 && renderer.RootText().Contains("pending change request"), "Pending leave change did not block submission");
    }

    private static AttendanceRequestApplicationDto Application(DateTime date, int? original = null) => new()
        { Date = DateOnly.FromDateTime(date), Time = new TimeOnly(8, 0), InOutModeId = 1, Comments = "Attendance correction test", OriginalLogId = original };
    private static AttendanceLog Clock(int id, int person, DateTime date) => new()
        { AttendanceLogId = id, IndividualId = person, OrganisationId = 10, Date = date.AddHours(10), InOutModeId = 1, AttendanceLogStateId = 1, Uidstamp = $"test-{id}" };
    private static async Task<Request> WriteRequest(HrmTeContext db, CreateRequestDto dto)
    {
        var request = new Request { RequestTypeId = dto.RequestTypeId, RequestStateId = 2,
            ApplicantBusinessEntityId = dto.ApplicantBusinessEntityId, OrganisationBusinessEntityID = dto.OrganisationBusinessEntityId,
            RequestEffectiveDate = dto.EffectiveDate, ReferenceNumber = Guid.NewGuid().ToString("N"), OperationLogId = dto.OperationLogId };
        db.Requests.Add(request);
        await db.SaveChangesAsync();
        return request;
    }
    private static async Task ApproveChange(RequestTestFactory factory, LeaveChangeService service, int balance, DateTime end, bool cancelled)
    {
        await using var db = factory.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var change = await db.LeaveChangeRequests.Include(x => x.Request).OrderByDescending(x => x.LeaveChangeRequestId).FirstAsync();
        var leave = await db.Leaves.Include(x => x.Job).SingleAsync(x => x.LeaveId == change.LeaveId);
        var apply = typeof(LeaveChangeService).GetMethod("ApplyApprovedChangeAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await (Task)apply.Invoke(service, [db, leave, change])!;
        change.Request.RequestStateId = 4;
        await db.SaveChangesAsync();
        Check((await db.JobLeaveTypes.SingleAsync()).RemainingDays == balance, "Approved return did not restore the exact unused balance");
        Check(leave.ToDate == end && (leave.LeaveStateId == 6) == cancelled, "Approved leave dates/state incorrect");
        await transaction.CommitAsync();
    }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
}

sealed class RequestTestPage : RequestPage
{
    protected override Task OnInitializedAsync() => Task.CompletedTask;
}

sealed class RequestTestFactory(DbContextOptions<HrmTeContext> options) : IDbContextFactory<HrmTeContext>
{
    public HrmTeContext CreateDbContext() => new RequestTestContext(options);
}

sealed class RequestTestContext(DbContextOptions<HrmTeContext> options) : HrmTeContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        Type[] types = [typeof(Job), typeof(Leaf), typeof(LeaveType), typeof(JobLeaveType), typeof(Request), typeof(RequestState),
            typeof(AttendanceLog), typeof(AttendanceLogResolution), typeof(AttendanceLogRequest), typeof(AttendanceLogChangeRequest),
            typeof(LeaveChangeRequest), typeof(LeaveTypeMapping), typeof(LeaveDefinition),
            typeof(HRM.Models.WorkPlanning.JobWorkTemplateAssignment), typeof(JobWorkTemplate), typeof(HRM.Models.WorkPlanning.WorkPlan), typeof(HRM.Models.WorkPlanning.WorkAssignment),
            typeof(HRM.Models.LeaveTypes.JobTypeLeaveAccrualRule), typeof(HRM.Models.LeaveTypes.LeaveAccrualPosting), typeof(HRM.Models.LeaveTypes.LeaveCarryForward)];
        types = types.Concat(ScheduleTestModel.EligibilityTypes).Distinct().ToArray();
        var navigations = new HashSet<(Type, string)>
        {
            (typeof(Leaf), "Job"), (typeof(Leaf), "LeaveType"), (typeof(Request), "RequestState"),
            (typeof(AttendanceLog), "AttendanceLogResolutions"), (typeof(AttendanceLogResolution), "AttendanceLog"),
            (typeof(AttendanceLogRequest), "AttendanceLog"), (typeof(AttendanceLogRequest), "Request"),
            (typeof(AttendanceLogChangeRequest), "AttendanceLog"), (typeof(AttendanceLogChangeRequest), "Request"),
            (typeof(LeaveChangeRequest), "Leave"), (typeof(LeaveChangeRequest), "Request"),
            (typeof(LeaveTypeMapping), "LeaveDefinition")
        };
        foreach (var type in builder.Model.GetEntityTypes().Select(x => x.ClrType).ToArray())
            if (!types.Contains(type)) builder.Ignore(type);
        foreach (var type in types)
        {
            builder.Entity(type).HasKey(type == typeof(HRM.Models.LeaveTypes.JobTypeLeaveAccrualRule) || type == typeof(HRM.Models.LeaveTypes.LeaveAccrualPosting) || type == typeof(HRM.Models.LeaveTypes.LeaveCarryForward)
                ? "Id" : type == typeof(HRM.Models.WorkPlanning.JobWorkTemplateAssignment) ? "JobWorkTemplateAssignmentID" : type == typeof(Leaf) ? "LeaveId" : type == typeof(AttendanceLogChangeRequest) ? "RequestId" : type.Name + "Id");
            foreach (var property in type.GetProperties().Where(x => !x.PropertyType.IsValueType && x.PropertyType != typeof(string) && x.PropertyType != typeof(byte[])))
                if (!navigations.Contains((type, property.Name)) && !ScheduleTestModel.Navigation(type, property.Name)) builder.Entity(type).Ignore(property.Name);
        }
        builder.Entity<Leaf>().HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId);
        builder.Entity<Leaf>().HasOne(x => x.LeaveType).WithMany().HasForeignKey(x => x.LeaveTypeId);
        builder.Entity<Request>().HasOne(x => x.RequestState).WithMany().HasForeignKey(x => x.RequestStateId);
        builder.Entity<AttendanceLogResolution>().HasOne(x => x.AttendanceLog).WithMany(x => x.AttendanceLogResolutions).HasForeignKey(x => x.AttendanceLogId);
        builder.Entity<AttendanceLogRequest>().HasOne(x => x.AttendanceLog).WithMany().HasForeignKey(x => x.AttendanceLogId);
        builder.Entity<AttendanceLogRequest>().HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId);
        builder.Entity<AttendanceLogChangeRequest>().HasOne(x => x.AttendanceLog).WithMany().HasForeignKey(x => x.AttendanceLogId);
        builder.Entity<AttendanceLogChangeRequest>().HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId);
        builder.Entity<LeaveChangeRequest>().HasOne(x => x.Leave).WithMany().HasForeignKey(x => x.LeaveId);
        builder.Entity<LeaveChangeRequest>().HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId);
        builder.Entity<LeaveTypeMapping>().HasOne(x => x.LeaveDefinition).WithMany().HasForeignKey(x => x.LeaveDefinitionId);
        ScheduleTestModel.Configure(builder);
    }
}
