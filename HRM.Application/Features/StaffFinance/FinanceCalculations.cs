namespace HRM.Application.Features.StaffFinance;

public static class FinanceCalculations
{
    public static List<(DateTime DueDate, decimal Amount)> Schedule(decimal total, DateTime firstDue, int count, int months)
    {
        if (total <= 0 || total > 999999999m || decimal.Round(total, 2) != total || count is < 1 or > 120 ||
            months is not (1 or 6 or 12) || firstDue.Year is < 2000 or > 2100 || count * months > 1200)
            throw new InvalidOperationException("Enter a positive amount with two decimals, a valid first due date, 1–120 instalments and monthly, six-monthly or annual frequency.");
        var amount = decimal.Round(total / count, 2, MidpointRounding.AwayFromZero);
        if (amount <= 0 || total - amount * (count - 1) <= 0) throw new InvalidOperationException("The total is too small for this instalment count.");
        return Enumerable.Range(0, count).Select(i => (firstDue.Date.AddMonths(i * months), i == count - 1 ? total - amount * (count - 1) : amount)).ToList();
    }
    public static decimal Prorate(decimal amount, DateTime month, DateTime start, DateTime? end)
    {
        var first = new DateTime(month.Year, month.Month, 1); var last = first.AddMonths(1).AddDays(-1);
        var from = start.Date > first ? start.Date : first; var to = end?.Date < last ? end.Value.Date : last;
        return to < from ? 0 : decimal.Round(amount * ((to - from).Days + 1) / DateTime.DaysInMonth(first.Year, first.Month), 2, MidpointRounding.AwayFromZero);
    }
    public static decimal MonthlyCap(decimal basicSalary, decimal fixedLimit, decimal percent, decimal minimumTakeHome) =>
        Math.Max(0, Math.Min(fixedLimit, Math.Min(basicSalary * percent / 100m, basicSalary - minimumTakeHome)));
}
