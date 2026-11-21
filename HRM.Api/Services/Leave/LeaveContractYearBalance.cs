using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Leave;

public static class LeaveContractYearBalance
{
    public static async Task<string?> ValidateSpendAsync(HrmTeContext db, JobLeaveType balance, DateTime leaveThrough)
    {
        var job = await db.Jobs.AsNoTracking().SingleOrDefaultAsync(x => x.JobId == balance.JobId);
        if (job == null) return null;
        var rule = await db.JobTypeLeaveAccrualRules.AsNoTracking().SingleOrDefaultAsync(x => x.OrganisationId == job.OrganisationID &&
            x.JobTypeId == job.JobTypeId && x.LeaveTypeId == balance.LeaveTypeId && x.IsEnabled && x.CarryForwardMode != "Unspecified");
        if (rule == null) return null;

        // Only block on anniversary boundaries that have already been reached (<= Today),
        // so advance leave applications crossing a future anniversary are not blocked before HR can process carry-forward.
        var today = LeaveAccrualCalculator.Today;
        var checkThrough = leaveThrough.Date < today ? leaveThrough.Date : today;
        foreach (var boundary in LeaveAccrualCalculator.DueDates(rule, job.JoinedDate, checkThrough).Where(x => x.Renewal))
        {
            if (balance.RenewedDate?.Date > boundary.Date) continue;
            if (await db.LeaveCarryForwards.AnyAsync(x => x.RuleId == rule.Id && x.JobId == job.JobId && x.RenewalDate == boundary.Date)) continue;
            // With no previous balance, first entitlement can be posted without carry-forward.
            if (await db.LeaveAccrualPostings.AnyAsync(x => x.RuleId == rule.Id && x.JobId == job.JobId && x.DueDate == boundary.Date)) continue;
            return $"The previous contract year's leave must be used by {boundary.Date.AddDays(-1):dd MMM yyyy}. HR must process carry-forward at {boundary.Date:dd MMM yyyy} before this balance can be used in the next contract year.";
        }
        return null;
    }
}
