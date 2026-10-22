using HRM.DTOs.UserContext;
using HRM.Enum;
using HRM.Helpers;
using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;

static class AttendanceEligibilityTests
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static async Task RunAsync()
    {
        var start = new DateTime(2026, 1, 1);
        var factory = new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false)).Options);
        await using(var db = factory.CreateDbContext())
        {
            db.Jobs.AddRange(new Job { JobId = 1, IndividualID = 100, OrganisationID = 10, JobStateId = 4, JoinedDate = start },
                new Job { JobId = 2, IndividualID = 100, OrganisationID = 20, JobStateId = 4, JoinedDate = start });
            db.JobPositions.Add(new JobPosition { JobPositionId = 1, PositionId = 1, JobId = 1, FromDate = start, JobPositionStateId = 4 });
            db.Requests.Add(new Request { RequestId = 1, RequestTypeId = 32, RequestStateId = 4, LastStateChangedDate = start.AddDays(4).AddHours(3) });
            db.JobPositionsRequests.Add(new JobPositionsRequest { JobPositionsRequestId = 1, JobPositionId = 1, RequestId = 1 });
            db.WorkTemplates.Add(new WorkTemplate { WorkTemplateId = 1, Name = "Official", RequiresAttendance = true, IsActive = true });
            db.JobWorkTemplateAssignments.Add(new JobWorkTemplateAssignment { JobWorkTemplateAssignmentID = 1, JobID = 1, WorkTemplateID = 1,
                IsActive = true, EffectiveFrom = start.AddDays(2), EffectiveTo = start.AddDays(9), WorkingDaysMask = 31 });
            await db.SaveChangesAsync();
        }
        await using(var db = factory.CreateDbContext())
        {
            var days = await AttendanceEligibilityCalendar.LoadAsync(db, 100, start, start.AddDays(14), 10);
            Check(days[start].Message == "No active schedule found" && !days[start].CanBeAbsent, "Before schedule was counted as absent");
            Check(days[start.AddDays(3)].HasActiveSchedule && !days[start.AddDays(3)].HasApprovedPosition && !days[start.AddDays(3)].CanBeAbsent,
                "Backdated joining/position date bypassed the actual approval date");
            Check(days[start.AddDays(4)].CanBeAbsent, "Attendance did not start on approval day with an active schedule");
            Check(!days[start.AddDays(8)].CanBeAbsent, "Normal Friday was treated as absent");
            Check(!days[start.AddDays(10)].HasActiveSchedule && !days[start.AddDays(10)].CanBeAbsent, "Expired schedule continued absence tracking");
            var otherTenant = await AttendanceEligibilityCalendar.LoadAsync(db, 100, start, start.AddDays(14), 20);
            Check(otherTenant.Values.All(x => !x.CanBeAbsent && !x.HasActiveSchedule), "Foreign tenant's position/schedule enabled attendance");
        }
        var access = Proxy.For<IUserAccessService>((method, _) => method == "RequireContextAsync" ? Task.FromResult(new UserContextDto { IndividualId = 100 }) : throw new Exception(method));
        var service = new AttendanceService(factory, null!, null!, access, null!, NullLogger<AttendanceService>.Instance);
        var overview = await service.GetMyYearlyAttendanceAsync(2026);
        Check(overview.Where(x => x.Date < start.AddDays(4) || x.Date > start.AddDays(9)).All(x => !x.IsAbsent), "Overview showed absences outside approval/schedule dates");
        Check(overview.Single(x => x.Date == start.AddDays(4)).IsAbsent, "Scheduled working day after approval was not tracked");
        var evaluator = new HRM.Services.Attendance.AttendanceEvaluationService(factory);
        var beforeApproval = await evaluator.EvaluateAsync(100, 1, start.AddDays(3));
        Check(beforeApproval.Status == AttendanceDailyStatus.BeforePositionApproval && !beforeApproval.RequiresAttendance, "Daily evaluation penalised an unapproved position");
        var noSchedule = await evaluator.EvaluateAsync(100, 1, start.AddDays(10));
        Check(noSchedule.Status == AttendanceDailyStatus.NoActiveSchedule && noSchedule.Issues.Any(x => x.Message == "No active schedule found"), "Missing schedule message not returned");
        using(var provider = new ServiceCollection().AddLogging().BuildServiceProvider())
        {
            await using var renderer = new TestRenderer(provider);
            await renderer.Dispatcher.InvokeAsync(async () =>
            {
                renderer.Attach(new HRM.Components.Pages.Online.Dashboard.AttendanceSummaryCard());
                await renderer.Render(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Days"] = overview,
                    ["WeeklyLogs"] = new List<HRM.DTOs.Attendance.AttendanceLogDto>() }));
            });
            renderer.CheckErrors();
            Check(renderer.RedWeeklyDayCount() == 0 && renderer.RootText().Contains("No active schedule found"), "Weekly day cards still showed false red absences without a schedule");
        }
        await using(var db = factory.CreateDbContext())
        {
            var assignment = await db.JobWorkTemplateAssignments.SingleAsync();
            assignment.ScheduleMode = "Rotation"; assignment.RotationTemplateIds = "[1,1,1,0]"; assignment.RotationAnchorDate = start.AddDays(4);
            await db.SaveChangesAsync();
        }
        overview = await service.GetMyYearlyAttendanceAsync(2026);
        Check(!overview.Single(x => x.Date == start.AddDays(7)).IsAbsent && overview.Single(x => x.Date == start.AddDays(8)).IsAbsent,
            "Rotation rest days or Friday duty ignored the shift cycle");
        await using(var db = factory.CreateDbContext())
        {
            db.WorkPlans.Add(new WorkPlan { WorkPlanId = 1, JobId = 1, IndividualId = 100, OrganisationBusinessEntityId = 10,
                WorkDate = start.AddDays(11), IsValid = true, IsManual = true,
                WorkPlanSegments = [new() { Name = "Manual check in", IsValid = true, RequiresAttendance = true,
                    StartDateTime = start.AddDays(11).AddHours(8), EndDateTime = start.AddDays(11).AddHours(9) }] });
            await db.SaveChangesAsync();
            var days = await AttendanceEligibilityCalendar.LoadAsync(db, 100, start.AddDays(11), start.AddDays(12), 10);
            Check(days[start.AddDays(11)].CanBeAbsent && !days[start.AddDays(12)].CanBeAbsent, "Manual duty enabled attendance beyond its actual date");
            (await db.JobPositions.SingleAsync()).JobPositionStateId = 3; await db.SaveChangesAsync();
            days = await AttendanceEligibilityCalendar.LoadAsync(db, 100, start.AddDays(11), start.AddDays(11), 10);
            Check(!days.Values.Single().CanBeAbsent, "A manual schedule bypassed position approval");
        }
    }
}
