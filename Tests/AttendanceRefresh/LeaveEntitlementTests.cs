using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.JobLeaveTypes;
using HRM.Services.Interfaces.Profile;
using HRM.Services.JobLeaveTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

static class LeaveEntitlementTests
{
    static void Check(bool value, string message) { if(!value) throw new Exception(message); }
    public static async Task RunAsync()
    {
        var factory = new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false)).Options);
        var actor = new UserContextDto { IndividualId = 26 };
        var access = Proxy.For<IUserAccessService>((method, _) => method switch {
            "RequireContextAsync" => Task.FromResult(actor), "GetCurrentOrganisationIdAsync" => Task.FromResult(3),
            "HasOrganisationAccessAsync" => Task.FromResult(true), "HasPermissionAsync" => Task.FromResult(false), _ => throw new Exception(method) });
        int staffArgument = 0;
        var jobs = Proxy.For<IJobService>((method, args) => method == "GetActiveJobAsync" ? Active((int)args[0]!) : throw new Exception(method));
        Task<ActiveJobDto?> Active(int staff) { staffArgument = staff; return Task.FromResult<ActiveJobDto?>(new() { JobId = 906, IndividualId = staff, IsActive = true }); }
        var audit = Proxy.For<IOperationLogService>((method, _) => throw new Exception(method));
        var profiles = Proxy.For<IProfileService>((method, _) => throw new Exception(method));
        var service = new JobLeaveTypeService(factory, audit, access, profiles, jobs);
        var today = DateTime.UtcNow.AddHours(5).Date;
        await using(var db = factory.CreateDbContext())
        {
            db.Jobs.AddRange(new Job { JobId = 906, IndividualID = 26, OrganisationID = 3 }, new Job { JobId = 907, IndividualID = 27, OrganisationID = 4 });
            db.LeaveTypes.AddRange(new LeaveType { LeaveTypeId = 1, Name = "Annual leave" }, new LeaveType { LeaveTypeId = 2, Name = "Upcoming leave" });
            db.JobLeaveTypes.AddRange(
                new JobLeaveType { JobLeaveTypeId = 1, JobId = 906, LeaveTypeId = 1, IsValid = true, IsLeaveInfoUpdated = true, RemainingDays = 17.5m, EffectiveFromDate = today.AddMonths(-3), EffectiveToDate = today.AddMonths(9), RenewedDate = today },
                new JobLeaveType { JobLeaveTypeId = 2, JobId = 906, LeaveTypeId = 2, IsValid = true, IsLeaveInfoUpdated = false, RemainingDays = 0, EffectiveFromDate = today.AddDays(10) },
                new JobLeaveType { JobLeaveTypeId = 3, JobId = 906, LeaveTypeId = 1, IsValid = true, IsLeaveInfoUpdated = true, RemainingDays = 4, EffectiveToDate = today.AddDays(-1) },
                new JobLeaveType { JobLeaveTypeId = 4, JobId = 906, LeaveTypeId = 1, IsValid = false, RemainingDays = 999 });
            await db.SaveChangesAsync();
        }
        var rows = await service.GetEntitlementsAsync(906);
        Check(rows.Count == 3 && rows.Any(x => !x.IsLeaveInfoUpdated), "Valid unconfirmed entitlements were hidden or invalid rows leaked");
        Check((await service.GetJobLeaveType(906)).Count == 2, "Unconfirmed balances became available in the leave application selector");
        try { await service.GetJobLeaveType(907); throw new Exception("Foreign tenant balance access allowed"); } catch(UnauthorizedAccessException) { }
        actor.IndividualId = 99;
        try { await service.GetJobLeaveType(906); throw new Exception("Unrelated staff balance access allowed"); } catch(UnauthorizedAccessException) { }
        actor.IndividualId = 26;
        using var provider = new ServiceCollection().AddLogging().AddSingleton<Microsoft.AspNetCore.Components.IComponentActivator, TestActivator>().BuildServiceProvider();
        await using var renderer = new TestRenderer(provider);
        var card = new HRM.Components.Profile.Employment.ProfileLeaveTypes { StaffId = 26 };
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        card.GetType().GetProperty("JobService", flags)!.SetValue(card, jobs);
        card.GetType().GetProperty("JobLeaveTypeService", flags)!.SetValue(card, service);
        await renderer.Dispatcher.InvokeAsync(async () => { renderer.Attach(card); await renderer.Render(); }); renderer.CheckErrors();
        var text = System.Text.RegularExpressions.Regex.Replace(renderer.RootText(), @"\s+", " ");
        Check(staffArgument == 26 && text.Contains("Annual leave") && text.Contains("17.5"), "Staff ID was used as job ID or recorded balance not shown");
        Check(text.Contains("Upcoming") && text.Contains("Expired") && text.Contains("1 active") && text.Contains("awaiting confirmation"), "Effective periods or unconfirmed balances misrepresented");
        Check(text.Contains(today.AddMonths(9).ToString("dd MMM yyyy")) && !text.Contains("Allocated") && !text.Contains("Used"), "Dates missing or fabricated allocation/usage remains");
        await using(var db = factory.CreateDbContext()) { (await db.JobLeaveTypes.FindAsync(1))!.RemainingDays = 20m; await db.SaveChangesAsync(); }
        await renderer.Dispatcher.InvokeAsync(async () => await renderer.RootClick("Refresh")); renderer.CheckErrors();
        Check(renderer.RootText().Contains("20"), "Refresh did not reload the balance");
    }
}
