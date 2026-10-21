using HRM.Models;
using HRM.Models.LeaveTypes;
using HRM.Services.Leave;
using HRM.Services.Interfaces;
using HRM.DTOs.UserContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

static class LeaveAccrualTests
{
    public static async Task RunAsync()
    {
        var monthly = new JobTypeLeaveAccrualRule { GrantDays = 2.5m, FirstGrantAfterMonths = 1, RepeatEveryMonths = 1,
            CarryForwardMode = "Unlimited", EffectiveFrom = new DateTime(2024, 1, 1) };
        Check(!LeaveAccrualCalculator.DueDates(monthly, new(2024, 1, 31), new(2024, 2, 28)).Any(), "Monthly accrual granted before a completed month");
        Check(LeaveAccrualCalculator.DueDates(monthly, new(2024, 1, 31), new(2024, 3, 31)).Select(x => x.Date).SequenceEqual([new DateTime(2024, 2, 29), new DateTime(2024, 3, 31)]), "Month-end/leap-year dates drifted");
        Check(LeaveAccrualCalculator.ContractYear(new(2026, 1, 1), new(2027, 1, 1)) == (new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)), "January contract year has the wrong use-by date");
        Check(!LeaveAccrualCalculator.DueDates(monthly, new(2026, 1, 1), new(2026, 12, 31)).Any(x => x.Renewal), "Carry-forward became due before the contract year ended");
        Check(LeaveAccrualCalculator.DueDates(monthly, new(2026, 1, 1), new(2027, 1, 1)).Count(x => x.IsGrant) == 12, "Contract year did not earn twelve monthly credits");
        Check(LeaveAccrualCalculator.IsCompletedPeriodCredit(monthly), "Final monthly credit was assigned to the new year");
        Check(LeaveAccrualCalculator.ContractYear(new(2024, 2, 29), new(2028, 2, 29)) == (new DateTime(2027, 2, 28), new DateTime(2028, 2, 28)), "Leap-day contract year dates drifted");
        var offset = new JobTypeLeaveAccrualRule { GrantDays = 2.5m, FirstGrantAfterMonths = 2, RepeatEveryMonths = 5, EffectiveFrom = new(2026, 1, 1) };
        var boundary = LeaveAccrualCalculator.DueDates(offset, new(2026, 1, 1), new(2027, 1, 1)).Single(x => x.Renewal);
        Check(boundary.Date == new DateTime(2027, 1, 1) && boundary.IsGrant, "Renewal did not follow the contract anniversary");
        offset.FirstGrantAfterMonths = 1;
        Check(!LeaveAccrualCalculator.DueDates(offset, new(2026, 1, 1), new(2027, 1, 1)).Single(x => x.Renewal).IsGrant, "Off-cycle schedule invented a renewal grant");
        var annual = new JobTypeLeaveAccrualRule { GrantDays = 30, FirstGrantAfterMonths = 12, RepeatEveryMonths = 12, CarryForwardMode = "Unlimited", EffectiveFrom = new(2024, 1, 1) };
        Check(!LeaveAccrualCalculator.DueDates(annual, new(2024, 2, 29), new(2025, 2, 27)).Any(), "Permanent entitlement granted before twelve months");
        Check(LeaveAccrualCalculator.DueDates(annual, new(2024, 2, 29), new(2025, 2, 28)).Count() == 1, "First annual entitlement missing");
        annual.FirstGrantAfterMonths = 0;
        Check(LeaveAccrualCalculator.DueDates(annual, new(2024, 2, 29), new(2024, 2, 29)).Count() == 1, "Political entitlement not available on joining");
        annual.CarryForwardMode = "Capped"; annual.CarryForwardLimit = 10;
        Check(LeaveAccrualCalculator.Apply(annual, 12, true) == (40m, 30m, -2m), "Capped carry-forward incorrect");
        annual.CarryForwardMode = "None";
        Check(LeaveAccrualCalculator.Apply(annual, 12, true).NewBalance == 30, "No-carry renewal incorrect");
        monthly.MaximumBalance = 10;
        Check(LeaveAccrualCalculator.Apply(monthly, 9.5m, false).NewBalance == 10, "Maximum balance cap incorrect");
        monthly.CarryForwardMode = "Unspecified";
        var blocked = false;
        try { LeaveAccrualCalculator.Apply(monthly, 1, false); } catch (InvalidOperationException) { blocked = true; }
        Check(blocked, "Unconfirmed carry-forward was processed");

        var options = new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
        var factory = new ReceiptFactory(options);
        var today = LeaveAccrualCalculator.Today;
        await using (var db = factory.CreateDbContext())
        {
            db.LeaveTypes.Add(new LeaveType { LeaveTypeId = 1, Name = "Annual Leave", OrganisationId = 10 });
            db.LeaveTypes.Add(new LeaveType { LeaveTypeId = 2, Name = "Annual Leave", OrganisationId = 10 });
            db.JobTypes.AddRange(new JobType { JobTypeId = 1, TypeName = "Contract" }, new JobType { JobTypeId = 9, TypeName = "Internship" });
            db.Organisations.AddRange(new Organisation { BusinessEntityID = 10, OrganisationName = "Tenant A" }, new Organisation { BusinessEntityID = 20, OrganisationName = "Tenant B" });
            db.LeaveTypes.Add(new LeaveType { LeaveTypeId = 3, Name = "Annual Leave", OrganisationId = 20 });
            db.JobTypeLeaveAccrualRules.Add(new JobTypeLeaveAccrualRule { Id = 2, OrganisationId = 20, JobTypeId = 1, LeaveTypeId = 3,
                GrantDays = 3, FirstGrantAfterMonths = 1, RepeatEveryMonths = 1, CarryForwardMode = "Unlimited", EffectiveFrom = today });
            db.JobTypeLeaveAccrualRules.Add(new JobTypeLeaveAccrualRule { Id = 1, OrganisationId = 10, JobTypeId = 1, LeaveTypeId = 1,
                GrantDays = 2.5m, FirstGrantAfterMonths = 1, RepeatEveryMonths = 1, CarryForwardMode = "Unlimited", EffectiveFrom = today, IsEnabled = true });
            db.Jobs.AddRange(new Job { JobId = 100, IndividualID = 1000, OrganisationID = 10, JobTypeId = 1, JobStateId = 4, JoinedDate = today.AddMonths(-1) },
                new Job { JobId = 101, IndividualID = 1001, OrganisationID = 20, JobTypeId = 1, JobStateId = 4, JoinedDate = today.AddMonths(-1) },
                new Job { JobId = 102, IndividualID = 1002, OrganisationID = 10, JobTypeId = 1, JobStateId = 4, JoinedDate = today.AddMonths(-1) });
            db.JobLeaveTypes.Add(new JobLeaveType { JobLeaveTypeId = 1, JobId = 100, LeaveTypeId = 1, RemainingDays = 5.5m, IsValid = true });
            db.JobLeaveTypes.Add(new JobLeaveType { JobLeaveTypeId = 2, JobId = 102, LeaveTypeId = 2, RemainingDays = 30m, IsValid = true });
            await db.SaveChangesAsync();
        }
        var processor = new LeaveAccrualProcessor(factory);
        var preview = await processor.PreviewAsync(10, 1);
        Check(preview.Single(x => x.JobId == 100).NewBalance == 8m, "Preview did not preserve the opening balance and fraction");
        await using (var db = factory.CreateDbContext()) Check((await db.JobLeaveTypes.SingleAsync(x => x.JobId == 100)).RemainingDays == 5.5m, "Preview changed a staff balance");
        Check(await processor.RunAsync(10, 1) == 1, "Tenant posting or equivalent-type reconciliation failed");
        Check(await processor.RunAsync(10, 1) == 0, "Repeated run duplicated credits");
        await using (var db = factory.CreateDbContext())
        {
            Check((await db.JobLeaveTypes.SingleAsync(x => x.JobId == 100)).RemainingDays == 8m, "Fractional accrual incorrect");
            Check(!await db.JobLeaveTypes.AnyAsync(x => x.JobId == 101), "Another tenant received a credit");
            Check((await db.JobLeaveTypes.SingleAsync(x => x.JobId == 102)).RemainingDays == 30m, "Equivalent legacy balance was overwritten");
            Check(await db.LeaveAccrualPostings.CountAsync() == 1, "Ledger is not idempotent");
        }
        var actor = new UserContextDto { UserId = 1, IndividualId = 1000 };
        var access = Proxy.For<IUserAccessService>((method, values) => method switch {
            "RequireContextAsync" => Task.FromResult(actor),
            "GetCurrentOrganisationIdAsync" => Task.FromResult(10),
            "HasOrganisationAccessAsync" => Task.FromResult((int)values[0]! == 10),
            "HasPermissionAsync" => Task.FromResult(true),
            _ => throw new Exception(method) });
        var logs = Proxy.For<IOperationLogService>((method, values) => method == "CreateAsync"
            ? Task.FromResult(new OperationLog { OperationLogId = 99 }) : throw new Exception(method));
        var settings = new LeaveAccrualSettingsService(factory, access, logs);
        Check((await settings.LoadAsync()).JobTypes.Any(x => x.Name == "Internship"), "New job types are hardcoded or missing");
        var internship = new JobTypeLeaveAccrualRule { OrganisationId = 20, JobTypeId = 9, LeaveTypeId = 1, GrantDays = 1.25m,
            FirstGrantAfterMonths = 1, RepeatEveryMonths = 1, EffectiveFrom = today, IsEnabled = true };
        blocked = false;
        try { await settings.SaveAsync(internship); } catch (InvalidOperationException) { blocked = true; }
        Check(blocked, "Settings enabled an unconfirmed carry-forward policy");
        internship.IsEnabled = false;
        await settings.SaveAsync(internship);
        await using (var db = factory.CreateDbContext()) Check((await db.JobTypeLeaveAccrualRules.SingleAsync(x => x.JobTypeId == 9)).OrganisationId == 10,
            "Browser-supplied tenant ID was trusted");
        var foreign = new JobTypeLeaveAccrualRule { Id = 2, OrganisationId = 10, JobTypeId = 1, LeaveTypeId = 1, GrantDays = 30,
            FirstGrantAfterMonths = 12, RepeatEveryMonths = 12, EffectiveFrom = today };
        blocked = false;
        try { await settings.SaveAsync(foreign); } catch (UnauthorizedAccessException) { blocked = true; }
        Check(blocked, "Tenant A could edit tenant B's rule");
        var hrAccess = Proxy.For<IUserAccessService>((method, values) => method switch {
            "RequireContextAsync" => Task.FromResult(actor),
            "GetCurrentOrganisationIdAsync" => Task.FromResult(10),
            "HasOrganisationAccessAsync" => Task.FromResult((int)values[0]! == 10),
            "HasUserGroupAsync" => Task.FromResult(true),
            "HasPermissionAsync" => Task.FromResult(true),
            _ => throw new Exception(method) });
        var carry = new LeaveCarryForwardService(factory, hrAccess, logs);
        await using (var db = factory.CreateDbContext())
        {
            var job = await db.Jobs.SingleAsync(x => x.JobId == 100);
            job.JoinedDate = today.AddYears(-1);
            db.LeaveAccrualPostings.RemoveRange(await db.LeaveAccrualPostings.ToListAsync());
            var rule = await db.JobTypeLeaveAccrualRules.SingleAsync(x => x.Id == 1);
            rule.CarryForwardMode = "Capped"; rule.CarryForwardLimit = 5;
            await db.SaveChangesAsync();
        }
        Check(await processor.RunAsync(10, 1) == 0, "Renewal changed balance before HR review");
        await using (var db = factory.CreateDbContext())
            Check(await LeaveContractYearBalance.ValidateSpendAsync(db, await db.JobLeaveTypes.SingleAsync(x => x.JobId == 100), today) != null,
                "Old contract-year balance was usable in the new year without HR carry-forward");
        var review = (await carry.PendingAsync()).Single(x => x.JobId == 100);
        Check(review.ClosingGrant == 2.5m && review.UnusedBalance == 10.5m && review.RenewalGrant == 0 && review.AnnualEntitlement == 30,
            "Final monthly earning was omitted or thirty days were granted upfront");
        blocked = false;
        try { await carry.ProcessAsync(review, 6, "Over limit"); } catch (InvalidOperationException) { blocked = true; }
        Check(blocked, "HR exceeded the configured carry limit");
        blocked = false;
        try { await carry.ProcessAsync(review with { Balance = 7 }, 4.25m, "Stale"); } catch (InvalidOperationException) { blocked = true; }
        Check(blocked, "Stale carry preview accepted");
        blocked = false;
        try { await carry.ProcessAsync(review with { RuleId = 2 }, 4, "Other tenant"); } catch (UnauthorizedAccessException) { blocked = true; }
        Check(blocked, "Carry-forward crossed tenants");
        var deniedAccess = Proxy.For<IUserAccessService>((method, values) => method == "RequireContextAsync" ? Task.FromResult(actor) : method == "HasUserGroupAsync" ? Task.FromResult(false) : throw new Exception(method));
        blocked = false;
        try { await new LeaveCarryForwardService(factory, deniedAccess, logs).PendingAsync(); } catch (UnauthorizedAccessException) { blocked = true; }
        Check(blocked, "Non-HR accessed carry-forward");
        var viewOnlyAccess = Proxy.For<IUserAccessService>((method, values) => method switch {
            "RequireContextAsync" => Task.FromResult(actor),
            "GetCurrentOrganisationIdAsync" => Task.FromResult(10),
            "HasOrganisationAccessAsync" => Task.FromResult((int)values[0]! == 10),
            "HasUserGroupAsync" => Task.FromResult(true),
            "HasPermissionAsync" => Task.FromResult((string)values[0]! == LeaveCarryForwardPermissions.View),
            _ => throw new Exception(method) });
        var viewOnly = new LeaveCarryForwardService(factory, viewOnlyAccess, logs);
        Check((await viewOnly.PendingAsync()).Any(), "View-only HR could not view carry-forward");
        blocked = false;
        try { await viewOnly.ProcessAsync(review, 4, "Read-only HR"); } catch (UnauthorizedAccessException) { blocked = true; }
        Check(blocked, "View-only HR processed carry-forward");
        var noRolesAccess = Proxy.For<IUserAccessService>((method, values) => method == "RequireContextAsync" ? Task.FromResult(actor) :
            method == "HasUserGroupAsync" ? Task.FromResult(true) : method == "HasPermissionAsync" ? Task.FromResult(false) : throw new Exception(method));
        blocked = false;
        try { await new LeaveCarryForwardService(factory, noRolesAccess, logs).HistoryAsync(); } catch (UnauthorizedAccessException) { blocked = true; }
        Check(blocked, "HR without carry-forward View role accessed history");
        await carry.ProcessAsync(review, 4.25m, "HR reviewed unused days");
        blocked = false;
        try { await carry.ProcessAsync(review, 4.25m, "Duplicate"); } catch (InvalidOperationException) { blocked = true; }
        Check(blocked, "Duplicate carry-forward accepted");
        Check(await processor.RunAsync(10, 1) == 0, "Final monthly credit was posted again after HR decision");
        Check(await processor.RunAsync(10, 1) == 0, "Renewal grant duplicated after carry-forward");
        await using (var db = factory.CreateDbContext())
        {
            Check((await db.JobLeaveTypes.SingleAsync(x => x.JobId == 100)).RemainingDays == 4.25m, "Closing monthly credit was added again as new-year credit");
            Check(await db.LeaveCarryForwards.CountAsync() == 1, "Carry-forward audit missing or duplicated");
            Check(await LeaveContractYearBalance.ValidateSpendAsync(db, await db.JobLeaveTypes.SingleAsync(x => x.JobId == 100), today) == null,
                "HR-carried balance was blocked in the new contract year");
            var entry = await db.LeaveCarryForwards.SingleAsync();
            Check(entry.PreviousBalance == 10.5m && entry.ClosingGrant == 2.5m && entry.RenewalGrant == 0 && entry.PeriodEnd == today.AddDays(-1), "Closing-year audit incorrect");
            var posting = await db.LeaveAccrualPostings.SingleAsync();
            Check(posting.GrantedDays == 2.5m && posting.CarryAdjustment == -6.25m && posting.NewBalance == 4.25m, "Boundary ledger does not reconcile");
            // Verify annual policies add the next year's grant atomically alongside HR's carry.
            db.LeaveCarryForwards.Remove(entry); db.LeaveAccrualPostings.Remove(posting);
            var rule = await db.JobTypeLeaveAccrualRules.SingleAsync(x => x.Id == 1);
            rule.GrantDays = 30; rule.FirstGrantAfterMonths = 12; rule.RepeatEveryMonths = 12; rule.Version++;
            (await db.JobLeaveTypes.SingleAsync(x => x.JobId == 100)).RemainingDays = 8;
            await db.SaveChangesAsync();
        }
        var annualReview = (await carry.PendingAsync()).Single(x => x.JobId == 100);
        Check(annualReview.ClosingGrant == 0 && annualReview.RenewalGrantFor(5) == 30, "Annual renewal grant preview incorrect");
        await carry.ProcessAsync(annualReview, 5, "Annual carry-forward review");
        Check(await processor.RunAsync(10, 1) == 0, "Annual renewal grant duplicated after HR processing");
        await using (var db = factory.CreateDbContext())
            Check((await db.JobLeaveTypes.SingleAsync(x => x.JobId == 100)).RemainingDays == 35, "Annual carry-forward did not add to next year's thirty days");
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
