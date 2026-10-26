using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Models.LeaveTypes;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Leave;

public sealed record AccrualPreview(int JobId, DateTime? DueDate, decimal Grant, decimal PreviousBalance, decimal NewBalance, string Message);

public sealed class LeaveAccrualProcessor(IDbContextFactory<HrmTeContext> factory)
{
    public async Task<List<AccrualPreview>> PreviewAsync(int org, int ruleId)
    {
        await using var db = await factory.CreateDbContextAsync();
        var rule = await db.JobTypeLeaveAccrualRules.AsNoTracking().SingleAsync(x => x.Id == ruleId && x.OrganisationId == org);
        var jobs = await db.Jobs.AsNoTracking().Where(j => j.OrganisationID == org && j.JobTypeId == rule.JobTypeId &&
            j.JobStateId == 4 && j.TerminatedDate == null && j.JoinedDate <= LeaveAccrualCalculator.Today).ToListAsync();
        var result = new List<AccrualPreview>();
        var leaveName = await db.LeaveTypes.Where(x => x.LeaveTypeId == rule.LeaveTypeId).Select(x => x.Name).SingleAsync();
        foreach (var job in jobs)
        {
            var balances = await db.JobLeaveTypes.AsNoTracking().Where(x => x.JobId == job.JobId && x.IsValid &&
                (x.LeaveTypeId == rule.LeaveTypeId || x.LeaveType.Name == leaveName)).ToListAsync();
            var balance = balances.SingleOrDefaultIfOne();
            if (balances.Count > 1) { result.Add(new(job.JobId, null, 0, 0, 0, "Multiple valid balances: reconcile before processing.")); continue; }
            if (balance != null && balance.LeaveTypeId != rule.LeaveTypeId) { result.Add(new(job.JobId, null, 0, balance.RemainingDays ?? 0, balance.RemainingDays ?? 0, "A different type with the same name is assigned; ask HR to reconcile before processing.")); continue; }
            var existing = await db.LeaveAccrualPostings.Where(x => x.RuleId == rule.Id && x.JobId == job.JobId).Select(x => x.DueDate).ToListAsync();
            var due = LeaveAccrualCalculator.DueDates(rule, job.JoinedDate, LeaveAccrualCalculator.Today).Where(x => !existing.Contains(x.Date)).ToList();
            var amount = balance?.RemainingDays ?? 0;
            if (due.Count == 0) result.Add(new(job.JobId, null, 0, amount, amount, "No unposted accrual is due."));
            foreach (var item in due)
            {
                if (balance?.RenewedDate?.Date > item.Date) { result.Add(new(job.JobId, item.Date, 0, amount, amount, "Balance has a later renewal date: review before processing.")); continue; }
                if (rule.CarryForwardMode == "Unspecified") { result.Add(new(job.JobId, item.Date, rule.GrantDays, amount, amount, "Carry-forward not configured; no credits can be posted.")); continue; }
                if (item.Renewal && balance != null && !await db.LeaveCarryForwards.AnyAsync(x => x.RuleId == ruleId && x.JobId == job.JobId && x.RenewalDate == item.Date))
                {
                    result.Add(new(job.JobId, item.Date, 0, amount, amount, "Awaiting HR carry-forward review.")); break;
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
        var jobs = await lookup.Jobs.Where(j => j.OrganisationID == org && j.JobTypeId == rule.JobTypeId && j.JobStateId == 4 &&
            j.TerminatedDate == null && j.JoinedDate <= LeaveAccrualCalculator.Today).Select(x => x.JobId).ToListAsync();
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
        var job = await db.Jobs.SingleOrDefaultAsync(j => j.JobId == jobId && j.OrganisationID == org && j.JobTypeId == rule.JobTypeId &&
            j.JobStateId == 4 && j.TerminatedDate == null && j.JoinedDate <= LeaveAccrualCalculator.Today);
        if (job == null) return 0;
        var leaveName = await db.LeaveTypes.Where(x => x.LeaveTypeId == rule.LeaveTypeId).Select(x => x.Name).SingleAsync();
        var balances = await db.JobLeaveTypes.Where(x => x.JobId == jobId && x.IsValid &&
            (x.LeaveTypeId == rule.LeaveTypeId || x.LeaveType.Name == leaveName)).ToListAsync();
        if (balances.Count > 1) return 0; // Reconciliation is required; do not guess or overwrite a staff balance.
        var balance = balances.SingleOrDefault();
        if (balance != null && balance.LeaveTypeId != rule.LeaveTypeId) return 0;
        var existing = await db.LeaveAccrualPostings.Where(x => x.RuleId == ruleId && x.JobId == jobId).Select(x => x.DueDate).ToListAsync();
        var count = 0;
        foreach (var due in LeaveAccrualCalculator.DueDates(rule, job.JoinedDate, LeaveAccrualCalculator.Today).Where(x => !existing.Contains(x.Date)))
        {
            if (balance?.RenewedDate?.Date > due.Date) continue;
            var before = balance?.RemainingDays ?? 0;
            if (due.Renewal && balance != null && !await db.LeaveCarryForwards.AnyAsync(x => x.RuleId == ruleId && x.JobId == jobId && x.RenewalDate == due.Date)) break;
            var next = due.IsGrant ? LeaveAccrualCalculator.Apply(rule, before, false) : (NewBalance: before, Granted: 0m, CarryAdjustment: 0m);
            var log = new OperationLog { OperationLogActionId = SharedConfig.OperationLogActionTypes.JOB_LEAVE_TYPE_UDPATE,
                CreatedByContextId = 1, UpdatedContextId = 1, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow,
                CreatedByIp = "", UpdatedByIp = "", Remarks = $"Automatic leave accrual: tenant {org}, rule {rule.Id} v{rule.Version}, job {jobId}, due {due.Date:yyyy-MM-dd}, grant {next.Granted}, carry adjustment {next.CarryAdjustment}." };
            db.OperationLogs.Add(log); await db.SaveChangesAsync();
            if (balance == null)
            {
                balance = new JobLeaveType { JobId = jobId, LeaveTypeId = rule.LeaveTypeId, IsValid = true,
                    EffectiveFromDate = due.Date, IsLeaveInfoUpdated = true, OperationLogId = log.OperationLogId };
                db.JobLeaveTypes.Add(balance);
            }
            balance.RemainingDays = next.NewBalance; balance.RenewedDate = due.Date;
            balance.EffectiveToDate = null; balance.OperationLogId = log.OperationLogId;
            await db.SaveChangesAsync();
            db.LeaveAccrualPostings.Add(new LeaveAccrualPosting { RuleId = ruleId, RuleVersion = rule.Version,
                JobId = jobId, JobLeaveTypeId = balance.JobLeaveTypeId, DueDate = due.Date, GrantedDays = next.Granted,
                CarryAdjustment = next.CarryAdjustment, PreviousBalance = before, NewBalance = next.NewBalance,
                PostedAtUtc = DateTime.UtcNow, OperationLogId = log.OperationLogId });
            await db.SaveChangesAsync(); count++;
        }
        await tx.CommitAsync(); return count;
    }
}

internal static class BalanceSelection
{
    public static T? SingleOrDefaultIfOne<T>(this List<T> list) where T : class => list.Count == 1 ? list[0] : null;
}
