using HRM.Models.LeaveTypes;

namespace HRM.Services.Leave;

public static class LeaveAccrualCalculator
{
    public static DateTime Today => DateTime.UtcNow.AddHours(5).Date;

    // Monthly/quarterly credits are earned in arrears. A credit on the anniversary
    // completes the year which just ended; annual grants open the new year.
    public static bool IsCompletedPeriodCredit(JobTypeLeaveAccrualRule rule) =>
        rule.FirstGrantAfterMonths > 0 && rule.RepeatEveryMonths < 12;

    public static (DateTime Start, DateTime End) ContractYear(DateTime joined, DateTime renewal)
    {
        joined = joined.Date; renewal = renewal.Date;
        var years = renewal.Year - joined.Year;
        if (years < 1 || joined.AddYears(years) != renewal)
            throw new InvalidOperationException("Carry-forward must occur on the contract-year anniversary.");
        return (joined.AddYears(years - 1), renewal.AddDays(-1));
    }

    public static IEnumerable<(DateTime Date, bool Renewal, bool IsGrant)> DueDates(JobTypeLeaveAccrualRule rule, DateTime joined, DateTime through)
    {
        if (rule.FirstGrantAfterMonths < 0 || rule.RepeatEveryMonths < 1 || rule.GrantDays <= 0)
            throw new InvalidOperationException("Grant days and recurrence must be positive; waiting months cannot be negative.");
        joined = joined.Date;
        var dates = new SortedDictionary<DateTime, (bool Renewal, bool IsGrant)>();
        for (var year = 1; year <= 100 && joined.Year + year <= 9999; year++)
        {
            var renewal = joined.AddYears(year);
            if (renewal > through.Date) break;
            if (renewal >= rule.EffectiveFrom.Date) dates[renewal] = (true, false);
        }
        for (var month = rule.FirstGrantAfterMonths; month <= 1200; month += rule.RepeatEveryMonths)
        {
            if (joined.Year + month / 12 > 9999) break;
            var due = joined.AddMonths(month);
            if (due > through.Date) break;
            if (due < rule.EffectiveFrom.Date) continue;
            dates[due] = (dates.TryGetValue(due, out var value) && value.Renewal, true);
        }
        foreach (var item in dates) yield return (item.Key, item.Value.Renewal, item.Value.IsGrant);
    }

    public static (decimal NewBalance, decimal Granted, decimal CarryAdjustment) Apply(JobTypeLeaveAccrualRule rule, decimal balance, bool renewal)
    {
        if (rule.CarryForwardMode is not ("None" or "Unlimited" or "Capped"))
            throw new InvalidOperationException("Configure carry-forward before processing accrual.");
        if (rule.CarryForwardMode == "Capped" && (!rule.CarryForwardLimit.HasValue || rule.CarryForwardLimit < 0))
            throw new InvalidOperationException("Enter the carry-forward limit.");
        var carry = balance;
        if (renewal)
            carry = rule.CarryForwardMode switch { "None" => 0, "Capped" => Math.Min(balance, rule.CarryForwardLimit!.Value), _ => balance };
        var grant = rule.GrantDays;
        if (rule.MaximumBalance.HasValue) grant = Math.Max(0, Math.Min(grant, rule.MaximumBalance.Value - carry));
        return (decimal.Round(carry + grant, 2), decimal.Round(grant, 2), decimal.Round(carry - balance, 2));
    }
}
