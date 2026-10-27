using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Models.LeaveTypes;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Leave;



public sealed partial class LeaveCarryForwardService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access, IOperationLogService logs)
:global::HRM.Services.Leave.Contracts.LeaveCarryForwardServiceContracts,global::HRM.Services.Leave.Contracts.LeaveCarryForwardServiceContracts.IOperations{
    private async Task<int> RequireHrAsync(bool processing = false)
    {
        await access.RequireContextAsync();
        if (!await access.HasUserGroupAsync("Human Resource: Staff") && !await access.HasUserGroupAsync("Human Resource :Head"))
            throw new UnauthorizedAccessException("Only HR staff can process carry-forward.");
        if (!await access.HasPermissionAsync(LeaveCarryForwardPermissions.View) ||
            (processing && !await access.HasPermissionAsync(LeaveCarryForwardPermissions.Process)))
            throw new UnauthorizedAccessException(processing
                ? "The Leave.CarryForward.View and Leave.CarryForward.Process roles are required to process carry-forward."
                : "The Leave.CarryForward.View role is required to view carry-forward.");
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org)) throw new UnauthorizedAccessException("Tenant access denied.");
        return org;
    }

    public async Task<List<CarryForwardReview>> PendingAsync()
    {
        var org = await RequireHrAsync();
        await using var db = await factory.CreateDbContextAsync();
        var rules = await db.JobTypeLeaveAccrualRules.AsNoTracking().Where(x => x.OrganisationId == org && x.IsEnabled && x.CarryForwardMode != "Unspecified").ToListAsync();
        var result = new List<CarryForwardReview>();
        foreach (var rule in rules)
        {
            var jobs = await db.Jobs.AsNoTracking().Where(x => x.OrganisationID == org && x.JobTypeId == rule.JobTypeId && x.JobStateId == 4 && x.TerminatedDate == null && x.JoinedDate <= LeaveAccrualCalculator.Today).ToListAsync();
            var name = await db.LeaveTypes.Where(x => x.LeaveTypeId == rule.LeaveTypeId).Select(x => x.Name).SingleAsync();
            foreach (var job in jobs)
            {
                var balances = await db.JobLeaveTypes.AsNoTracking().Where(x => x.JobId == job.JobId && x.IsValid && (x.LeaveTypeId == rule.LeaveTypeId || x.LeaveType.Name == name)).ToListAsync();
                if (balances.Count != 1 || balances[0].LeaveTypeId != rule.LeaveTypeId) continue;
                var balance = balances[0];
                var posted = await db.LeaveAccrualPostings.Where(x => x.RuleId == rule.Id && x.JobId == job.JobId).Select(x => x.DueDate).ToListAsync();
                var next = LeaveAccrualCalculator.DueDates(rule, job.JoinedDate, LeaveAccrualCalculator.Today)
                    .FirstOrDefault(x => !posted.Contains(x.Date) && !(balance.RenewedDate?.Date > x.Date));
                if (!next.Renewal || await db.LeaveCarryForwards.AnyAsync(x => x.RuleId == rule.Id && x.JobId == job.JobId && x.RenewalDate == next.Date)) continue;
                var amount = balance.RemainingDays ?? 0;
                if (amount < 0) continue; // Negative balances require reconciliation, not a carry-forward write-off.
                var period = LeaveAccrualCalculator.ContractYear(job.JoinedDate, next.Date);
                var closing = next.IsGrant && LeaveAccrualCalculator.IsCompletedPeriodCredit(rule) ? LeaveAccrualCalculator.Apply(rule, amount, false).Granted : 0;
                var unused = amount + closing;
                var suggested = CarryMaximum(rule, unused);
                var openingGrant = next.IsGrant && !LeaveAccrualCalculator.IsCompletedPeriodCredit(rule) ? LeaveAccrualCalculator.Apply(rule, suggested, false).Granted : 0;
                var person = await db.Individuals.AsNoTracking().SingleOrDefaultAsync(x => x.BusinessEntityId == job.IndividualID);
                var staff = person == null ? $"Staff #{job.IndividualID}" : string.Join(" ", new[] { person.FirstNameEnglish, person.MiddleNameEnglish, person.LastNameEnglish }.Where(x => !string.IsNullOrWhiteSpace(x)));
                result.Add(new(rule.Id, rule.Version, job.JobId, balance.JobLeaveTypeId, $"{staff} · Job #{job.JobId}", name, next.Date, amount, suggested)
                {
                    PeriodStart = period.Start, ClosingGrant = closing, RenewalGrant = openingGrant,
                    ScheduledRenewalGrant = next.IsGrant && !LeaveAccrualCalculator.IsCompletedPeriodCredit(rule) ? rule.GrantDays : 0,
                    MaximumBalance = rule.MaximumBalance,
                    AnnualEntitlement = decimal.Round(rule.GrantDays * 12 / rule.RepeatEveryMonths, 2),
                    AccrualDescription = $"{rule.GrantDays:0.##} days every {rule.RepeatEveryMonths} month(s)" +
                        (LeaveAccrualCalculator.IsCompletedPeriodCredit(rule) ? ", after each completed period" : ", at the start of the entitlement period")
                });
            }
        }
        return result;
    }

    public async Task ProcessAsync(CarryForwardReview preview, decimal days, string reason)
    {
        var org = await RequireHrAsync(processing: true);
        var actor = await access.RequireContextAsync();
        if (days < 0 || decimal.Round(days, 2) != days || string.IsNullOrWhiteSpace(reason) || reason.Length > 1000)
            throw new InvalidOperationException("Enter non-negative days (two decimals) and a reason up to 1000 characters.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var rule = await db.JobTypeLeaveAccrualRules.SingleOrDefaultAsync(x => x.Id == preview.RuleId && x.OrganisationId == org && x.IsEnabled)
            ?? throw new UnauthorizedAccessException("Rule unavailable in this tenant.");
        var job = await db.Jobs.SingleOrDefaultAsync(x => x.JobId == preview.JobId && x.OrganisationID == org && x.JobTypeId == rule.JobTypeId && x.JobStateId == 4 && x.TerminatedDate == null)
            ?? throw new InvalidOperationException("Staff job is no longer eligible.");
        var balances = await db.JobLeaveTypes.Where(x => x.JobId == job.JobId && x.IsValid && (x.LeaveTypeId == rule.LeaveTypeId || x.LeaveType.Name == db.LeaveTypes.Where(t => t.LeaveTypeId == rule.LeaveTypeId).Select(t => t.Name).First())).ToListAsync();
        var balance = balances.Count == 1 ? balances[0] : null;
        if (balance == null || balance.JobLeaveTypeId != preview.BalanceId || balance.LeaveTypeId != rule.LeaveTypeId || (balance.RemainingDays ?? 0) != preview.Balance || preview.Balance < 0 || rule.Version != preview.RuleVersion)
            throw new InvalidOperationException("Balance or configuration changed. Reload and review again.");
        var posted = await db.LeaveAccrualPostings.Where(x => x.RuleId == rule.Id && x.JobId == job.JobId).Select(x => x.DueDate).ToListAsync();
        var next = LeaveAccrualCalculator.DueDates(rule, job.JoinedDate, LeaveAccrualCalculator.Today).FirstOrDefault(x => !posted.Contains(x.Date) && !(balance.RenewedDate?.Date > x.Date));
        if (!next.Renewal || next.Date != preview.RenewalDate || await db.LeaveCarryForwards.AnyAsync(x => x.RuleId == rule.Id && x.JobId == job.JobId && x.RenewalDate == next.Date))
            throw new InvalidOperationException("This renewal is not awaiting carry-forward review. Reload the list.");
        var period = LeaveAccrualCalculator.ContractYear(job.JoinedDate, next.Date);
        var completedPeriod = LeaveAccrualCalculator.IsCompletedPeriodCredit(rule);
        var closing = next.IsGrant && completedPeriod ? LeaveAccrualCalculator.Apply(rule, preview.Balance, false).Granted : 0;
        var unused = preview.Balance + closing;
        var maximum = CarryMaximum(rule, unused);
        if (days > maximum) throw new InvalidOperationException($"Carry-forward cannot exceed {maximum:0.##} days under this policy.");
        var opening = next.IsGrant && !completedPeriod ? LeaveAccrualCalculator.Apply(rule, days, false).Granted : 0;
        var finalBalance = days + opening;
        var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.JOB_LEAVE_TYPE_UDPATE,
            $"HR carry-forward: tenant {org}, job {job.JobId}, contract year {period.Start:yyyy-MM-dd} through {period.End:yyyy-MM-dd}, renewal {next.Date:yyyy-MM-dd}, closing credit {closing}, unused {unused}, carried {days}, new-year grant {opening}, new balance {finalBalance}. {reason.Trim()}");
        balance.RemainingDays = finalBalance; balance.RenewedDate = next.Date; balance.EffectiveToDate = null; balance.OperationLogId = log.OperationLogId;
        db.LeaveCarryForwards.Add(new() { RuleId = rule.Id, RuleVersion = rule.Version, JobId = job.JobId, JobLeaveTypeId = balance.JobLeaveTypeId,
            RenewalDate = next.Date, PreviousBalance = unused, CarriedDays = days, ProcessedByUserId = actor.UserId,
            PeriodStart = period.Start, PeriodEnd = period.End, ClosingGrant = closing, RenewalGrant = opening, NewBalance = finalBalance,
            ProcessedAtUtc = DateTime.UtcNow, Reason = reason.Trim(), OperationLogId = log.OperationLogId });
        // Complete the boundary credit and HR decision in one transaction so the
        // final completed month is never credited again as part of the new year.
        db.LeaveAccrualPostings.Add(new() { RuleId = rule.Id, RuleVersion = rule.Version, JobId = job.JobId,
            JobLeaveTypeId = balance.JobLeaveTypeId, DueDate = next.Date, GrantedDays = closing + opening,
            CarryAdjustment = days - unused, PreviousBalance = preview.Balance, NewBalance = finalBalance,
            PostedAtUtc = DateTime.UtcNow, OperationLogId = log.OperationLogId });
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }

    private static decimal CarryMaximum(JobTypeLeaveAccrualRule rule, decimal unused) => rule.CarryForwardMode switch
    {
        "None" => 0m,
        "Capped" => Math.Min(Math.Max(0, unused), rule.CarryForwardLimit ?? 0),
        "Unlimited" => Math.Max(0, unused),
        _ => throw new InvalidOperationException("Configure carry-forward first.")
    };

    public async Task<List<LeaveCarryForward>> HistoryAsync()
    {
        var org = await RequireHrAsync();
        await using var db = await factory.CreateDbContextAsync();
        return await db.LeaveCarryForwards.AsNoTracking().Where(x => db.JobTypeLeaveAccrualRules.Any(r => r.Id == x.RuleId && r.OrganisationId == org))
            .OrderByDescending(x => x.ProcessedAtUtc).Take(100).ToListAsync();
    }
}
