using System;
using System.Collections.Generic;
using HRM.DTOs.GoodsLoans;

namespace HRM.Application.Features.GoodsLoans;

public interface IGoodsLoanScheduleCalculator
{
    List<RepaymentScheduleInstalmentPlan> CalculateSchedule(decimal totalRepayableAmount, int repaymentMonths, DateTime firstDeductionMonth);
}

public sealed class GoodsLoanScheduleCalculator : IGoodsLoanScheduleCalculator
{
    public List<RepaymentScheduleInstalmentPlan> CalculateSchedule(decimal totalRepayableAmount, int repaymentMonths, DateTime firstDeductionMonth)
    {
        if (totalRepayableAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalRepayableAmount), "Total repayable amount must be greater than zero.");
        }

        if (repaymentMonths <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(repaymentMonths), "Repayment duration must be at least 1 month.");
        }

        // Normalize first deduction month to first day of that month
        var startMonth = new DateTime(firstDeductionMonth.Year, firstDeductionMonth.Month, 1);

        var schedule = new List<RepaymentScheduleInstalmentPlan>(repaymentMonths);

        if (repaymentMonths == 1)
        {
            schedule.Add(new RepaymentScheduleInstalmentPlan
            {
                InstalmentNumber = 1,
                DueMonth = startMonth,
                ScheduledAmount = decimal.Round(totalRepayableAmount, 2, MidpointRounding.AwayFromZero),
                RemainingPrincipalAfter = 0m
            });
            return schedule;
        }

        // Regular monthly amount rounded to 2 decimal places
        decimal regularAmount = decimal.Round(totalRepayableAmount / repaymentMonths, 2, MidpointRounding.AwayFromZero);

        // Sum of all normal months except the final month
        decimal accumulatedRegular = regularAmount * (repaymentMonths - 1);

        // Final month absorbs any rounding remainder
        decimal finalAmount = totalRepayableAmount - accumulatedRegular;

        // Ensure no negative or zero instalment caused by extreme rounding
        if (finalAmount <= 0)
        {
            // Fallback: distribute evenly with floor
            regularAmount = Math.Floor((totalRepayableAmount / repaymentMonths) * 100m) / 100m;
            accumulatedRegular = regularAmount * (repaymentMonths - 1);
            finalAmount = totalRepayableAmount - accumulatedRegular;
        }

        decimal remaining = totalRepayableAmount;
        for (int i = 1; i < repaymentMonths; i++)
        {
            remaining = Math.Max(0m, remaining - regularAmount);
            schedule.Add(new RepaymentScheduleInstalmentPlan
            {
                InstalmentNumber = i,
                DueMonth = startMonth.AddMonths(i - 1),
                ScheduledAmount = regularAmount,
                RemainingPrincipalAfter = remaining
            });
        }

        schedule.Add(new RepaymentScheduleInstalmentPlan
        {
            InstalmentNumber = repaymentMonths,
            DueMonth = startMonth.AddMonths(repaymentMonths - 1),
            ScheduledAmount = finalAmount,
            RemainingPrincipalAfter = 0m
        });

        return schedule;
    }
}
