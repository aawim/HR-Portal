using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Models.LeaveTypes;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Leave;

public sealed class LeaveAccrualProcessor(IDbContextFactory<HrmTeContext> factory)
{
    public async Task<List<AccrualPreview>> PreviewAsync(int org, int ruleId)
    {
        await using var db = await factory.CreateDbContextAsync();
        var rule = await db.JobTypeLeaveAccrualRules.AsNoTracking().SingleAsync(x => x.Id == ruleId && x.OrganisationId == org);
        var today = LeaveAccrualCalculator.Today;
        var jobs = await db.Jobs.AsNoTracking().Where(j => j.OrganisationID == org && j.JobTypeId == rule.JobTypeId &&
            j.JobStateId == 4 && (j.TerminatedDate == null || j.TerminatedDate >= today) && j.JoinedDate <= today).ToListAsync();
        var result = new List<AccrualPreview>();
        var leaveType = await db.LeaveTypes.AsNoTracking().SingleAsync(x => x.LeaveTypeId == rule.LeaveTypeId);
        if (!leaveType.IsActive) return result;
        var leaveName = leaveType.Name;
        foreach (var job in jobs)
        {
            var balances = await db.JobLeaveTypes.AsNoTracking().Where(x => x.JobId == job.JobId && x.IsValid &&
                (x.LeaveTypeId == rule.LeaveTypeId || x.LeaveType.Name == leaveName)).ToListAsync();
            var balance = balances.SingleOrDefaultIfOne();
            if (balances.Count > 1) { result.Add(new(job.JobId, null, 0, 0, 0, "Multiple valid balances: reconcile before processing.")); continue; }
            if (balance != null && balance.LeaveTypeId != rule.LeaveTypeId) { result.Add(new(job.JobId, null, 0, balance.RemainingDays ?? 0, balance.RemainingDays ?? 0, "A different type with the same name is assigned; ask HR to reconcile before processing.")); continue; }
            var existing = await db.LeaveAccrualPostings.Where(x => x.RuleId == rule.Id && x.JobId == job.JobId).Select(x => x.DueDate).ToListAsync();
            var effectiveThrough = job.TerminatedDate.HasValue && job.TerminatedDate.Value.Date < today
                ? job.TerminatedDate.Value.Date
                : today;
            var due = LeaveAccrualCalculator.DueDates(rule, job.JoinedDate, effectiveThrough).Where(x => !existing.Contains(x.Date)).ToList();
            var amount = balance?.RemainingDays ?? 0;
            if (due.Count == 0) result.Add(new(job.JobId, null, 0, amount, amount, "No unposted accrual is due."));
            foreach (var item in due)
            {
                if (balance == null && !item.IsGrant) continue;
                if (balance?.RenewedDate?.Date > item.Date) { result.Add(new(job.JobId, item.Date, 0, amount, amount, "Balance has a later renewal date: review before processing.")); continue; }
                if (rule.CarryForwardMode == "Unspecified") { result.Add(new(job.JobId, item.Date, rule.GrantDays, amount, amount, "Carry-forward not configured; no credits can be posted.")); continue; }
                if (item.Renewal && balance != null && !await db.LeaveCarryForwards.AnyAsync(x => x.RuleId == ruleId && x.JobId == job.JobId && x.RenewalDate == item.Date))
                {
                    if (amount < 0)
                    {
                        result.Add(new(job.JobId, item.Date, 0, amount, amount, $"Negative balance ({amount:0.##} days) requires HR reconciliation before renewal."));
                        break;
                    }
                    if (rule.CarryForwardMode == "Capped")
                    {
                        result.Add(new(job.JobId, item.Date, 0, amount, amount, "Awaiting HR carry-forward review."));
                        break;
                    }

                    // Auto-rollover preview for None / Unlimited modes
                    var (_, _, _, finalBalance, totalGrant) = ComputeAutoRollover(rule, amount, item.IsGrant);
                    result.Add(new(job.JobId, item.Date, totalGrant, amount, finalBalance,
                        rule.IsEnabled ? $"Ready (auto {rule.CarryForwardMode.ToLowerInvariant()} carry-forward)" : "Draft: enable rule to process."));
                    amount = finalBalance;
                    continue;
                }
                var next = item.IsGrant ? LeaveAccrualCalculator.Apply(rule, amount, false) : (NewBalance: amount, Granted: 0m, CarryAdjustment: 0m);
                result.Add(new(job.JobId, item.Date, next.Granted, amount, next.NewBalance, rule.IsEnabled ? "Ready" : "Draft: enable rule to process."));
                amount = next.NewBalance;
            }
        }
        return result;
    }

    public async Task<int> RunAsync(int org, int ruleId)
    {
        await using var lookup = await factory.CreateDbContextAsync();
        var rule = await lookup.JobTypeLeaveAccrualRules.AsNoTracking().SingleAsync(x => x.Id == ruleId && x.OrganisationId == org && x.IsEnabled);
        if (!await lookup.LeaveTypes.AnyAsync(x => x.LeaveTypeId == rule.LeaveTypeId && x.IsActive)) return 0;
        var today = LeaveAccrualCalculator.Today;
        var jobs = await lookup.Jobs.Where(j => j.OrganisationID == org && j.JobTypeId == rule.JobTypeId && j.JobStateId == 4 &&
            (j.TerminatedDate == null || j.TerminatedDate >= today) && j.JoinedDate <= today).Select(x => x.JobId).ToListAsync();
        var posted = 0;
        foreach (var id in jobs) posted += await ProcessJobAsync(org, ruleId, id);
        return posted;
    }

    private async Task<int> ProcessJobAsync(int org, int ruleId, int jobId)
    {
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var rule = await db.JobTypeLeaveAccrualRules.SingleAsync(x => x.Id == ruleId && x.OrganisationId == org);
        if (!await db.LeaveTypes.AnyAsync(x => x.LeaveTypeId == rule.LeaveTypeId && x.IsActive)) return 0;
        if (!rule.IsEnabled || rule.CarryForwardMode == "Unspecified") return 0;
        var today = LeaveAccrualCalculator.Today;
        var job = await db.Jobs.SingleOrDefaultAsync(j => j.JobId == jobId && j.OrganisationID == org && j.JobTypeId == rule.JobTypeId &&
            j.JobStateId == 4 && (j.TerminatedDate == null || j.TerminatedDate >= today) && j.JoinedDate <= today);
        if (job == null) return 0;
        var leaveName = await db.LeaveTypes.Where(x => x.LeaveTypeId == rule.LeaveTypeId).Select(x => x.Name).SingleAsync();
        var balances = await db.JobLeaveTypes.Where(x => x.JobId == jobId && x.IsValid &&
            (x.LeaveTypeId == rule.LeaveTypeId || x.LeaveType.Name == leaveName)).ToListAsync();
        if (balances.Count > 1) return 0; // Reconciliation is required; do not guess or overwrite a staff balance.
        var balance = balances.SingleOrDefault();
        if (balance != null && balance.LeaveTypeId != rule.LeaveTypeId) return 0;
        var existing = await db.LeaveAccrualPostings.Where(x => x.RuleId == ruleId && x.JobId == jobId).Select(x => x.DueDate).ToListAsync();
        var effectiveThrough = job.TerminatedDate.HasValue && job.TerminatedDate.Value.Date < today
            ? job.TerminatedDate.Value.Date
            : today;
        var systemContextId = await ResolveSystemContextIdAsync(db);
        var count = 0;
        foreach (var due in LeaveAccrualCalculator.DueDates(rule, job.JoinedDate, effectiveThrough).Where(x => !existing.Contains(x.Date)))
        {
            if (balance == null && !due.IsGrant) continue; // Do not create a 0-day balance row on a non-grant anniversary.
            if (balance?.RenewedDate?.Date > due.Date) continue;
            var before = balance?.RemainingDays ?? 0;

            if (due.Renewal && balance != null && !await db.LeaveCarryForwards.AnyAsync(x => x.RuleId == ruleId && x.JobId == jobId && x.RenewalDate == due.Date))
            {
                // Negative balances and Capped policies require explicit HR reconciliation/review.
                if (before < 0 || rule.CarryForwardMode == "Capped") break;

                // Automatic rollover for None (forfeit unused) and Unlimited (carry all) policies.
                var period = LeaveAccrualCalculator.ContractYear(job.JoinedDate, due.Date);
                var (closing, unused, carried, opening, finalBalance) = ComputeAutoRolloverDetails(rule, before, due.IsGrant);
                var autoLog = new OperationLog
                {
                    OperationLogActionId = SharedConfig.OperationLogActionTypes.JOB_LEAVE_TYPE_UDPATE,
                    CreatedByContextId = systemContextId,
                    UpdatedContextId = systemContextId,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    CreatedByIp = "",
                    UpdatedByIp = "",
                    Remarks = $"Automatic carry-forward ({rule.CarryForwardMode}): tenant {org}, rule {rule.Id} v{rule.Version}, job {jobId}, renewal {due.Date:yyyy-MM-dd}, closing {closing}, unused {unused}, carried {carried}, opening {opening}, new balance {finalBalance}."
                };
                db.OperationLogs.Add(autoLog);
                await db.SaveChangesAsync();

                balance.RemainingDays = finalBalance;
                balance.RenewedDate = due.Date;
                balance.EffectiveToDate = null;
                balance.OperationLogId = autoLog.OperationLogId;

                db.LeaveCarryForwards.Add(new LeaveCarryForward
                {
                    RuleId = rule.Id,
                    RuleVersion = rule.Version,
                    JobId = job.JobId,
                    JobLeaveTypeId = balance.JobLeaveTypeId,
                    RenewalDate = due.Date,
                    PreviousBalance = unused,
                    CarriedDays = carried,
                    ProcessedByUserId = 0,
                    PeriodStart = period.Start,
                    PeriodEnd = period.End,
                    ClosingGrant = closing,
                    RenewalGrant = opening,
                    NewBalance = finalBalance,
                    ProcessedAtUtc = DateTime.UtcNow,
                    Reason = $"Automatic carry-forward under {rule.CarryForwardMode} policy.",
                    OperationLogId = autoLog.OperationLogId
                });

                db.LeaveAccrualPostings.Add(new LeaveAccrualPosting
                {
                    RuleId = ruleId,
                    RuleVersion = rule.Version,
                    JobId = jobId,
                    JobLeaveTypeId = balance.JobLeaveTypeId,
                    DueDate = due.Date,
                    GrantedDays = decimal.Round(closing + opening, 2),
                    CarryAdjustment = decimal.Round(carried - unused, 2),
                    PreviousBalance = before,
                    NewBalance = finalBalance,
                    PostedAtUtc = DateTime.UtcNow,
                    OperationLogId = autoLog.OperationLogId
                });

                await db.SaveChangesAsync();
                count++;
                continue;
            }

            var next = due.IsGrant ? LeaveAccrualCalculator.Apply(rule, before, false) : (NewBalance: before, Granted: 0m, CarryAdjustment: 0m);
            var log = new OperationLog
            {
                OperationLogActionId = SharedConfig.OperationLogActionTypes.JOB_LEAVE_TYPE_UDPATE,
                CreatedByContextId = systemContextId,
                UpdatedContextId = systemContextId,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                CreatedByIp = "",
                UpdatedByIp = "",
                Remarks = $"Automatic leave accrual: tenant {org}, rule {rule.Id} v{rule.Version}, job {jobId}, due {due.Date:yyyy-MM-dd}, grant {next.Granted}, carry adjustment {next.CarryAdjustment}."
            };
            db.OperationLogs.Add(log);
            await db.SaveChangesAsync();
            if (balance == null)
            {
                balance = new JobLeaveType
                {
                    JobId = jobId,
                    LeaveTypeId = rule.LeaveTypeId,
                    IsValid = true,
                    EffectiveFromDate = due.Date,
                    IsLeaveInfoUpdated = true,
                    OperationLogId = log.OperationLogId
                };
                db.JobLeaveTypes.Add(balance);
            }
            balance.RemainingDays = next.NewBalance;
            balance.RenewedDate = due.Date;
            balance.EffectiveToDate = null;
            balance.OperationLogId = log.OperationLogId;
            await db.SaveChangesAsync();
            db.LeaveAccrualPostings.Add(new LeaveAccrualPosting
            {
                RuleId = ruleId,
                RuleVersion = rule.Version,
                JobId = jobId,
                JobLeaveTypeId = balance.JobLeaveTypeId,
                DueDate = due.Date,
                GrantedDays = next.Granted,
                CarryAdjustment = next.CarryAdjustment,
                PreviousBalance = before,
                NewBalance = next.NewBalance,
                PostedAtUtc = DateTime.UtcNow,
                OperationLogId = log.OperationLogId
            });
            await db.SaveChangesAsync();
            count++;
        }
        await tx.CommitAsync();
        return count;
    }

    private static (decimal Closing, decimal Unused, decimal Carried, decimal Opening, decimal FinalBalance) ComputeAutoRolloverDetails(
        JobTypeLeaveAccrualRule rule,
        decimal before,
        bool isGrant)
    {
        var completedPeriod = LeaveAccrualCalculator.IsCompletedPeriodCredit(rule);
        var closing = isGrant && completedPeriod ? LeaveAccrualCalculator.Apply(rule, before, false).Granted : 0m;
        var unused = decimal.Round(before + closing, 2);
        var carried = rule.CarryForwardMode == "None" ? 0m : Math.Max(0m, unused);
        var opening = isGrant && !completedPeriod ? LeaveAccrualCalculator.Apply(rule, carried, false).Granted : 0m;
        var finalBalance = decimal.Round(carried + opening, 2);
        return (closing, unused, carried, opening, finalBalance);
    }

    private static (decimal Closing, decimal Unused, decimal Carried, decimal FinalBalance, decimal TotalGrant) ComputeAutoRollover(
        JobTypeLeaveAccrualRule rule,
        decimal before,
        bool isGrant)
    {
        var (closing, unused, carried, opening, finalBalance) = ComputeAutoRolloverDetails(rule, before, isGrant);
        return (closing, unused, carried, finalBalance, decimal.Round(closing + opening, 2));
    }

    private static async Task<int> ResolveSystemContextIdAsync(HrmTeContext db)
    {
        try
        {
            var existingId = await db.OperationLogs.AsNoTracking()
                .Where(x => x.CreatedByContextId > 0)
                .OrderBy(x => x.CreatedByContextId)
                .Select(x => x.CreatedByContextId)
                .FirstOrDefaultAsync();
            return existingId > 0 ? existingId : 1;
        }
        catch
        {
            return 1;
        }
    }
}

internal static class BalanceSelection
{
    public static T? SingleOrDefaultIfOne<T>(this List<T> list) where T : class => list.Count == 1 ? list[0] : null;
}
