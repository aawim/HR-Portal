using HRM.Components.Pages.Online.Leave;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.LeaveTypes;
using HRM.Models.WorkPlanning;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.Leave;
using HRM.Services.Leave;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

static class LeaveReceiptTests
{
    public static async Task RunAsync()
    {
        var options = new DbContextOptionsBuilder<HrmTeContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false)).Options;
        var factory = new ReceiptFactory(options);
        var start = new DateTime(2026, 7, 19);
        await using (var db = factory.CreateDbContext())
        {
            db.Individuals.Add(new Individual { BusinessEntityId = 1, FirstNameEnglish = "Staff", LastNameEnglish = "Member" });
            db.Organisations.Add(new Organisation { BusinessEntityID = 10, OrganisationName = "Test Organisation", RegistrationNumber = "ORG-10" });
            db.Jobs.Add(new Job { JobId = 1, IndividualID = 1, OrganisationID = 10, Sapnumber = "EMP-1" });
            db.LeaveTypes.Add(new LeaveType { LeaveTypeId = 1, Name = "Annual Leave" });
            db.Users.Add(new User { UserId = 2, FullName = "HR Approver", Username = "hr" });
            db.RequestStates.AddRange(new RequestState { RequestStateId = 4, StateName = "Approved", IsFinalState = true },
                new RequestState { RequestStateId = 2, StateName = "Pending verification", IsProcessingState = true });
            db.Leaves.AddRange(
                new Leaf { LeaveId = 1, JobId = 1, LeaveTypeId = 1, LeaveStateId = 4, FromDate = start, ToDate = start.AddDays(2), Duration = 3 },
                new Leaf { LeaveId = 2, JobId = 1, LeaveTypeId = 1, LeaveStateId = 2, FromDate = start, ToDate = start },
                new Leaf { LeaveId = 3, JobId = 1, LeaveTypeId = 1, LeaveStateId = 6, FromDate = start, ToDate = start },
                new Leaf { LeaveId = 4, JobId = 1, LeaveTypeId = 1, LeaveStateId = 4, FromDate = start.AddDays(3), ToDate = start.AddDays(3), Duration = 1 },
                new Leaf { LeaveId = 5, JobId = 1, LeaveTypeId = 1, LeaveStateId = 4, FromDate = start, ToDate = start, Duration = 1 });
            db.Requests.AddRange(new Request { RequestId = 1, RequestStateId = 4, ReferenceNumber = "REF-1", LastStateChangedByUserId = 2, LastStateChangedDate = start.AddDays(-1) },
                new Request { RequestId = 5, RequestStateId = 2, LastStateChangedByUserId = 2 });
            db.LeaveRequests.AddRange(new LeaveRequest { RequestId = 1, LeaveId = 1 }, new LeaveRequest { RequestId = 5, LeaveId = 5 });
            db.JobLeaveTypes.Add(new JobLeaveType { JobLeaveTypeId = 1, JobId = 1, LeaveTypeId = 1, IsValid = true, RemainingDays = 17 });
            for (var i = 1; i <= 2; i++)
            {
                db.WorkPlans.Add(new WorkPlan { WorkPlanId = i, JobId = 1, IndividualId = 1, OrganisationBusinessEntityId = 10, IsValid = true, WorkDate = start.AddDays(i + 2) });
                db.WorkPlanSegments.Add(new WorkPlanSegment { WorkPlanSegmentId = i, WorkPlanId = i, IsValid = true, RequiresAttendance = true,
                    StartDateTime = start.AddDays(i + 2).AddHours(8), EndDateTime = start.AddDays(i + 2).AddHours(16) });
            }
            await db.SaveChangesAsync();
        }
        var user = new UserContextDto { IndividualId = 1, ActiveJob = new ActiveJobDto { JobId = 99 } };
        var access = Proxy.For<IUserAccessService>((_, _) => Task.FromResult(user));
        var service = new LeaveReceiptService(factory, access);
        var receipt = await service.GetMyReceiptAsync(1) ?? throw new Exception("Approved receipt missing");
        Check(receipt.StaffName == "Staff Member" && receipt.Organisation == "Test Organisation" && receipt.Reference == "REF-1", "Letterhead or staff/reference incorrect");
        Check(receipt.FromDate == start && receipt.ToDate == start.AddDays(2) && receipt.Days == 3 && receipt.CurrentBalance == 17, "Receipt dates, days or balance incorrect");
        Check(receipt.ApprovedBy == "HR Approver" && receipt.ApprovedAt == start.AddDays(-1), "Approval details incorrect");
        Check(receipt.ReportingDate == start.AddDays(4), "Reporting date did not skip other approved leave");
        Check(receipt.CalendarReturnDate == start.AddDays(3), "Calendar return date is not the day after leave");
        Check(await service.GetMyReceiptAsync(2) == null && await service.GetMyReceiptAsync(3) == null, "Unapproved receipt exposed");
        Check((await service.GetMyReceiptAsync(4))?.ApprovedBy == null && (await service.GetMyReceiptAsync(5))?.ApprovedBy == null,
            "Legacy approval details were invented from a pending request");
        using var services = new ServiceCollection().AddLogging().AddSingleton<ILeaveReceiptService>(service)
            .AddSingleton(Proxy.For<IJSRuntime>((_, _) => throw new Exception("Unexpected JavaScript call"))).BuildServiceProvider();
        await using (var renderer = new TestRenderer(services))
        {
            var page = new LeaveReceipt();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            typeof(LeaveReceipt).GetProperty("Receipts", flags)!.SetValue(page, service);
            typeof(LeaveReceipt).GetProperty("Logger", flags)!.SetValue(page, Microsoft.Extensions.Logging.Abstractions.NullLogger<LeaveReceipt>.Instance);
            await renderer.Dispatcher.InvokeAsync(async () => { renderer.Attach(page); await renderer.Render(ParameterView.FromDictionary(new Dictionary<string, object?> { ["LeaveId"] = 1 })); });
            renderer.CheckErrors();
            Check(renderer.RootText().Contains("HR Approver") && renderer.RootText().Contains("Current leave balance") && renderer.RootButtonCount("Print / Save as PDF") == 1,
                "Receipt page fields or print action missing");
        }
        await using (var db = factory.CreateDbContext())
        {
            db.WorkPlanSegments.RemoveRange(db.WorkPlanSegments);
            db.JobLeaveTypes.Add(new JobLeaveType { JobLeaveTypeId = 2, JobId = 1, LeaveTypeId = 1, IsValid = true, RemainingDays = 20 });
            await db.SaveChangesAsync();
        }
        var incompleteData = await service.GetMyReceiptAsync(1);
        Check(incompleteData?.ReportingDate == null && incompleteData?.CurrentBalance == null,
            "Receipt guessed a reporting date or an ambiguous leave balance");
        Check(incompleteData?.CalendarReturnDate == start.AddDays(3), "Missing schedule hid the calendar return date");
        user.IndividualId = 2;
        Check(await service.GetMyReceiptAsync(1) == null, "Another employee can access staff receipt");
        user.IndividualId = 0;
        Check(await service.GetMyReceiptAsync(1) == null, "Receipt accessible without staff identity");
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}

sealed class ReceiptFactory(DbContextOptions<HrmTeContext> options) : IDbContextFactory<HrmTeContext>
{
    public HrmTeContext CreateDbContext() => new SelfLeaveContext(options);
}
