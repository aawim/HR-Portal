using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.LeaveTypes;
using HRM.Services.Interfaces;
using HRM.Services.Leave;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class LeaveAccrualEdgeCaseTests
{
    private sealed class TestFactory(DbContextOptions<HrmTeContext> options) : IDbContextFactory<HrmTeContext>
    {
        public HrmTeContext CreateDbContext() => new(options);
    }

    private static TestFactory CreateFactory()
    {
        var options = new DbContextOptionsBuilder<HrmTeContext>()
            .UseInMemoryDatabase($"LeaveAccrualEdge_{Guid.NewGuid():N}", x => x.EnableNullChecks(false))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new TestFactory(options);
    }

    [Fact]
    public async Task AutoRollover_NoneMode_ForfeitsUnusedAndGrantsAnnualOpeningAtomically()
    {
        var factory = CreateFactory();
        var today = LeaveAccrualCalculator.Today;

        await using (var db = factory.CreateDbContext())
        {
            db.LeaveTypes.Add(new LeaveType { LeaveTypeId = 1, Name = "Annual Leave", OrganisationId = 10, IsActive = true });
            db.JobTypeLeaveAccrualRules.Add(new JobTypeLeaveAccrualRule
            {
                Id = 1, OrganisationId = 10, JobTypeId = 1, LeaveTypeId = 1,
                GrantDays = 30m, FirstGrantAfterMonths = 12, RepeatEveryMonths = 12,
                CarryForwardMode = "None", EffectiveFrom = today.AddDays(-5), IsEnabled = true
            });
            db.Jobs.Add(new Job { JobId = 100, IndividualID = 1, OrganisationID = 10, JobTypeId = 1, JobStateId = 4, JoinedDate = today.AddYears(-1) });
            db.JobLeaveTypes.Add(new JobLeaveType { JobLeaveTypeId = 1, JobId = 100, LeaveTypeId = 1, RemainingDays = 12m, IsValid = true });
            await db.SaveChangesAsync();
        }

        var processor = new LeaveAccrualProcessor(factory);
        Assert.Equal(1, await processor.RunAsync(10, 1));
        Assert.Equal(0, await processor.RunAsync(10, 1)); // Idempotent

        await using (var db = factory.CreateDbContext())
        {
            var balance = await db.JobLeaveTypes.SingleAsync(x => x.JobId == 100);
            Assert.Equal(30m, balance.RemainingDays);
            var cf = await db.LeaveCarryForwards.SingleAsync();
            Assert.Equal(12m, cf.PreviousBalance);
            Assert.Equal(0m, cf.CarriedDays);
            Assert.Equal(30m, cf.RenewalGrant);
            Assert.Equal(30m, cf.NewBalance);
        }
    }

    [Fact]
    public async Task AutoRollover_UnlimitedMode_CarriesMonthlyClosingCreditWithoutDoubleCounting()
    {
        var factory = CreateFactory();
        var today = LeaveAccrualCalculator.Today;

        await using (var db = factory.CreateDbContext())
        {
            db.LeaveTypes.Add(new LeaveType { LeaveTypeId = 1, Name = "Annual Leave", OrganisationId = 10, IsActive = true });
            db.JobTypeLeaveAccrualRules.Add(new JobTypeLeaveAccrualRule
            {
                Id = 1, OrganisationId = 10, JobTypeId = 1, LeaveTypeId = 1,
                GrantDays = 2.5m, FirstGrantAfterMonths = 1, RepeatEveryMonths = 1,
                CarryForwardMode = "Unlimited", EffectiveFrom = today, IsEnabled = true
            });
            db.Jobs.Add(new Job { JobId = 100, IndividualID = 1, OrganisationID = 10, JobTypeId = 1, JobStateId = 4, JoinedDate = today.AddYears(-1) });
            db.JobLeaveTypes.Add(new JobLeaveType { JobLeaveTypeId = 1, JobId = 100, LeaveTypeId = 1, RemainingDays = 8m, IsValid = true });
            await db.SaveChangesAsync();
        }

        var processor = new LeaveAccrualProcessor(factory);
        Assert.Equal(1, await processor.RunAsync(10, 1));
        Assert.Equal(0, await processor.RunAsync(10, 1));

        await using (var db = factory.CreateDbContext())
        {
            var balance = await db.JobLeaveTypes.SingleAsync(x => x.JobId == 100);
            Assert.Equal(10.5m, balance.RemainingDays);
            var cf = await db.LeaveCarryForwards.SingleAsync();
            Assert.Equal(2.5m, cf.ClosingGrant);
            Assert.Equal(10.5m, cf.CarriedDays);
            Assert.Equal(0m, cf.RenewalGrant);
        }
    }

    [Fact]
    public async Task NegativeBalance_PausesRenewalAndSurfacesWarningInPreview()
    {
        var factory = CreateFactory();
        var today = LeaveAccrualCalculator.Today;

        await using (var db = factory.CreateDbContext())
        {
            db.LeaveTypes.Add(new LeaveType { LeaveTypeId = 1, Name = "Annual Leave", OrganisationId = 10, IsActive = true });
            db.JobTypeLeaveAccrualRules.Add(new JobTypeLeaveAccrualRule
            {
                Id = 1, OrganisationId = 10, JobTypeId = 1, LeaveTypeId = 1,
                GrantDays = 30m, FirstGrantAfterMonths = 12, RepeatEveryMonths = 12,
                CarryForwardMode = "None", EffectiveFrom = today, IsEnabled = true
            });
            db.Jobs.Add(new Job { JobId = 100, IndividualID = 1, OrganisationID = 10, JobTypeId = 1, JobStateId = 4, JoinedDate = today.AddYears(-1) });
            db.JobLeaveTypes.Add(new JobLeaveType { JobLeaveTypeId = 1, JobId = 100, LeaveTypeId = 1, RemainingDays = -2m, IsValid = true });
            await db.SaveChangesAsync();
        }

        var processor = new LeaveAccrualProcessor(factory);
        var preview = await processor.PreviewAsync(10, 1);
        Assert.Contains(preview, p => p.JobId == 100 && p.Message.Contains("Negative balance", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, await processor.RunAsync(10, 1));
    }

    [Fact]
    public async Task FutureScheduledTermination_StillAccruesUntilTerminationDate()
    {
        var factory = CreateFactory();
        var today = LeaveAccrualCalculator.Today;

        await using (var db = factory.CreateDbContext())
        {
            db.LeaveTypes.Add(new LeaveType { LeaveTypeId = 1, Name = "Annual Leave", OrganisationId = 10, IsActive = true });
            db.JobTypeLeaveAccrualRules.Add(new JobTypeLeaveAccrualRule
            {
                Id = 1, OrganisationId = 10, JobTypeId = 1, LeaveTypeId = 1,
                GrantDays = 2.5m, FirstGrantAfterMonths = 1, RepeatEveryMonths = 1,
                CarryForwardMode = "Unlimited", EffectiveFrom = today, IsEnabled = true
            });
            db.Jobs.Add(new Job
            {
                JobId = 100, IndividualID = 1, OrganisationID = 10, JobTypeId = 1, JobStateId = 4,
                JoinedDate = today.AddMonths(-1),
                TerminatedDate = today.AddDays(30) // Future separation date
            });
            db.JobLeaveTypes.Add(new JobLeaveType { JobLeaveTypeId = 1, JobId = 100, LeaveTypeId = 1, RemainingDays = 5m, IsValid = true });
            await db.SaveChangesAsync();
        }

        var processor = new LeaveAccrualProcessor(factory);
        Assert.Equal(1, await processor.RunAsync(10, 1));

        await using (var db = factory.CreateDbContext())
            Assert.Equal(7.5m, (await db.JobLeaveTypes.SingleAsync(x => x.JobId == 100)).RemainingDays);
    }

    [Fact]
    public async Task ValidateSpendAsync_AllowsAdvanceLeaveCrossingFutureAnniversary()
    {
        var factory = CreateFactory();
        var today = LeaveAccrualCalculator.Today;

        await using var db = factory.CreateDbContext();
        db.LeaveTypes.Add(new LeaveType { LeaveTypeId = 1, Name = "Annual Leave", OrganisationId = 10, IsActive = true });
        db.JobTypeLeaveAccrualRules.Add(new JobTypeLeaveAccrualRule
        {
            Id = 1, OrganisationId = 10, JobTypeId = 1, LeaveTypeId = 1,
            GrantDays = 30m, FirstGrantAfterMonths = 12, RepeatEveryMonths = 12,
            CarryForwardMode = "Capped", CarryForwardLimit = 5m, EffectiveFrom = today.AddMonths(-1), IsEnabled = true
        });
        // Anniversary is 20 days in the future
        db.Jobs.Add(new Job { JobId = 100, IndividualID = 1, OrganisationID = 10, JobTypeId = 1, JobStateId = 4, JoinedDate = today.AddYears(-1).AddDays(20) });
        var balance = new JobLeaveType { JobLeaveTypeId = 1, JobId = 100, LeaveTypeId = 1, RemainingDays = 10m, IsValid = true };
        db.JobLeaveTypes.Add(balance);
        await db.SaveChangesAsync();

        // Requesting leave that extends 30 days into the future (crossing upcoming anniversary)
        var error = await LeaveContractYearBalance.ValidateSpendAsync(db, balance, today.AddDays(30));
        Assert.Null(error);
    }

    [Fact]
    public async Task NonGrantAnniversary_WithNoExistingBalance_DoesNotCreateZeroDayGhostBalance()
    {
        var factory = CreateFactory();
        var today = LeaveAccrualCalculator.Today;

        await using (var db = factory.CreateDbContext())
        {
            db.LeaveTypes.Add(new LeaveType { LeaveTypeId = 1, Name = "Long Service", OrganisationId = 10, IsActive = true });
            db.JobTypeLeaveAccrualRules.Add(new JobTypeLeaveAccrualRule
            {
                Id = 1, OrganisationId = 10, JobTypeId = 1, LeaveTypeId = 1,
                GrantDays = 15m, FirstGrantAfterMonths = 18, RepeatEveryMonths = 12,
                CarryForwardMode = "None", EffectiveFrom = today.AddDays(-5), IsEnabled = true
            });
            db.Jobs.Add(new Job { JobId = 100, IndividualID = 1, OrganisationID = 10, JobTypeId = 1, JobStateId = 4, JoinedDate = today.AddYears(-1) });
            await db.SaveChangesAsync();
        }

        var processor = new LeaveAccrualProcessor(factory);
        Assert.Equal(0, await processor.RunAsync(10, 1));

        await using (var db = factory.CreateDbContext())
            Assert.Empty(await db.JobLeaveTypes.ToListAsync());
    }
}
