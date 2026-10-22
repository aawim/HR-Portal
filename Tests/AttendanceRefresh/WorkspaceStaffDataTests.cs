using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.Services.Leave;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;

static class WorkspaceStaffDataTests
{
    static void Check(bool value, string message) { if(!value) throw new Exception(message); }
    public static async Task RunAsync()
    {
        var factory = new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false)).Options);
        var actor = new UserContextDto { IndividualId = 100, IsHRStaff = true };
        var access = Proxy.For<IUserAccessService>((method, _) => method switch {
            "RequireContextAsync" => Task.FromResult(actor), "GetCurrentOrganisationIdAsync" => Task.FromResult(3), "HasOrganisationAccessAsync" => Task.FromResult(true),
            "HasPermissionAsync" => Task.FromResult(false), _ => throw new Exception(method) });
        var today = DateTime.UtcNow.AddHours(5).Date; var day = today.AddDays(-1);
        await using(var db = factory.CreateDbContext())
        {
            db.Jobs.AddRange(new Job { JobId = 906, IndividualID = 26, OrganisationID = 3, JobStateId = 4, JoinedDate = today.AddDays(-10) },
                new Job { JobId = 907, IndividualID = 26, OrganisationID = 4, JobStateId = 4, JoinedDate = today },
                new Job { JobId = 908, IndividualID = 27, OrganisationID = 4, JobStateId = 4, JoinedDate = today });
            db.LeaveTypes.Add(new LeaveType { LeaveTypeId = 1, Name = "Annual leave" });
            db.JobLeaveTypes.Add(new JobLeaveType { JobId = 906, LeaveTypeId = 1, IsValid = true, IsLeaveInfoUpdated = true, RemainingDays = 17.5m });
            db.Leaves.Add(new Leaf { JobId = 906, LeaveTypeId = 1, LeaveStateId = 4, FromDate = today.AddDays(-2), ToDate = today.AddDays(-2) });
            db.AttendanceLogs.AddRange(
                new AttendanceLog { IndividualId = 26, OrganisationId = 3, Date = day.AddHours(8), InOutModeId = 1, AttendanceLogStateId = 1,
                    AttendanceLogResolutions = [new() { IsValid = true, AttendanceClockTypeId = 1 }] },
                new AttendanceLog { IndividualId = 26, OrganisationId = 3, Date = day.AddHours(12), InOutModeId = 2, AttendanceLogStateId = 1,
                    AttendanceLogResolutions = [new() { IsValid = true, AttendanceClockTypeId = 2 }] },
                new AttendanceLog { IndividualId = 26, OrganisationId = 4, Date = day.AddHours(7), InOutModeId = 1, AttendanceLogStateId = 1 },
                new AttendanceLog { IndividualId = 100, OrganisationId = 3, Date = day.AddHours(6), InOutModeId = 1, AttendanceLogStateId = 1 });
            await db.SaveChangesAsync();
        }
        var attendance = new AttendanceService(factory, null!, null!, access, null!, NullLogger<AttendanceService>.Instance);
        var days = await attendance.GetStaffYearlyAttendanceAsync(26, today.Year);
        var present = days.Single(x => x.Date == day);
        Check(present.FirstCheckIn == day.AddHours(8) && present.LastCheckOut == day.AddHours(12) && present.ClockEventCount == 2, "Selected attendance mixed staff or tenants");
        Check(days.All(x => x.Date >= today.AddDays(-10)), "Attendance marked dates before joining as absent");
        Check(days.Single(x => x.Date == today.AddDays(-2)).LeaveTypeName == "Annual leave", "Selected employee's leave type not shown");
        Check(days.Where(x => x.Date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday).All(x => !x.IsAbsent), "Weekends count as absent");
        Check((await attendance.GetStaffClockEventsAsync(26, day, day)).Count == 2, "Clock events mixed tenants or employees");
        try { await attendance.GetStaffYearlyAttendanceAsync(27, today.Year); throw new Exception("Foreign tenant attendance allowed"); } catch(UnauthorizedAccessException) { }
        actor.IsHRStaff = false;
        try { await attendance.GetStaffClockEventsAsync(26, day, day); throw new Exception("Unrelated staff attendance allowed"); } catch(UnauthorizedAccessException) { }
        actor.IsHRStaff = true;
        var leaves = new LeaveService(factory, null!, null!, null!, access, null!, null!);
        Check(await leaves.GetJobIdByStaffId(26) == 906, "Leave manager selected another tenant's job or confused staff and job IDs");
        Check(await leaves.GetJobIdByStaffId(27) == 0, "Foreign tenant job was returned for leave assignment");
        using var provider = new ServiceCollection().AddLogging().AddSingleton<Microsoft.AspNetCore.Components.IComponentActivator, TestActivator>().BuildServiceProvider();
        await using var renderer = new TestRenderer(provider);
        var card = new HRM.Components.Admin.My.Workspace.Staff.Tabs.StaffAttendance { StaffId = 26 };
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        card.GetType().GetProperty("Attendance", flags)!.SetValue(card, attendance);
        await renderer.Dispatcher.InvokeAsync(async () => { renderer.Attach(card); await renderer.Render(); }); renderer.CheckErrors();
        Check(renderer.RootText().Contains("Annual leave") && renderer.RootText().Contains("Clock events") && !renderer.RootText().Contains("could not be loaded"), "Workspace attendance did not render selected employee data");
        await using var managerRenderer = new TestRenderer(provider);
        var manager = new HRM.Components.Admin.My.Workspace.Staff.Tabs.LeaveAssignmentManager { StaffId = 26 };
        var managerType = manager.GetType();
        managerType.GetProperty("LeaveService", flags)!.SetValue(manager, leaves);
        managerType.GetProperty("JobLeaveTypeService", flags)!.SetValue(manager, new HRM.Services.JobLeaveTypes.JobLeaveTypeService(factory, null!, access, null!, null!));
        managerType.GetProperty("leaveDefinitionService", flags)!.SetValue(manager, new LeaveDefinitionService(factory, access, null!));
        managerType.GetProperty("LeaveTypeService", flags)!.SetValue(manager, new LeaveTypeService(factory, access, NullLogger<LeaveTypeService>.Instance, null!, null!));
        managerType.GetProperty("ToastService", flags)!.SetValue(manager, new ToastService());
        await managerRenderer.Dispatcher.InvokeAsync(async () => { managerRenderer.Attach(manager); await managerRenderer.Render(); }); managerRenderer.CheckErrors();
        Check(manager.JobId == 906 && managerType.GetField("loadError", flags)!.GetValue(manager) == null, "Leave assignments could not load for staff ID different from job ID");
        Check(((List<HRM.DTOs.Leave.JobLeaveTypeDto>)managerType.GetField("assigned", flags)!.GetValue(manager)!).Single().RemainingDays == 17.5m, "Leave manager did not load the selected staff balance");
    }
}
