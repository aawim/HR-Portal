using System;
using System.Collections.Generic;
using System.Linq;
using HRM.Application.Features.GoodsLoans;
using HRM.Application.Features.Payroll;
using HRM.DTOs.GoodsLoans;
using HRM.Models.GoodsLoans;
using Xunit;

namespace HRM.Api.UnitTests.GoodsLoans;

public sealed class GoodsLoanManagementTests
{
    private readonly GoodsLoanScheduleCalculator _calculator = new();

    [Fact]
    public void ScheduleCalculator_EvenDivision_GeneratesIdenticalInstalments()
    {
        // Example: Store A MVR 12,000 for 24 months = 500.00 every month
        decimal principal = 12000m;
        int months = 24;
        var startMonth = new DateTime(2026, 12, 1);

        var schedule = _calculator.CalculateSchedule(principal, months, startMonth);

        Assert.Equal(24, schedule.Count);
        Assert.All(schedule, inst => Assert.Equal(500.00m, inst.ScheduledAmount));
        Assert.Equal(principal, schedule.Sum(i => i.ScheduledAmount));
        Assert.Equal(startMonth, schedule.First().DueMonth);
        Assert.Equal(startMonth.AddMonths(23), schedule.Last().DueMonth);
        Assert.Equal(0m, schedule.Last().RemainingPrincipalAfter);
    }

    [Fact]
    public void ScheduleCalculator_RoundingAbsorption_AllocatesRemainderToFinalInstalment()
    {
        // Example: Store B MVR 8,000 for 24 months
        // 8000 / 24 = 333.3333... -> regular = 333.33
        // 23 months * 333.33 = 7666.59
        // Final 24th month = 8000 - 7666.59 = 333.41
        decimal principal = 8000m;
        int months = 24;
        var startMonth = new DateTime(2027, 1, 1);

        var schedule = _calculator.CalculateSchedule(principal, months, startMonth);

        Assert.Equal(24, schedule.Count);

        // Verify first 23 instalments are exactly 333.33
        for (int i = 0; i < 23; i++)
        {
            Assert.Equal(i + 1, schedule[i].InstalmentNumber);
            Assert.Equal(333.33m, schedule[i].ScheduledAmount);
        }

        // Verify final 24th instalment is 333.41
        Assert.Equal(24, schedule[23].InstalmentNumber);
        Assert.Equal(333.41m, schedule[23].ScheduledAmount);

        // Verify total sum equals principal down to the cent
        Assert.Equal(8000.00m, schedule.Sum(s => s.ScheduledAmount));
        Assert.Equal(0m, schedule[23].RemainingPrincipalAfter);
    }

    [Fact]
    public void ScheduleCalculator_SingleMonth_ReturnsEntireAmount()
    {
        decimal principal = 1500.75m;
        var schedule = _calculator.CalculateSchedule(principal, 1, new DateTime(2026, 12, 1));

        Assert.Single(schedule);
        Assert.Equal(1500.75m, schedule[0].ScheduledAmount);
        Assert.Equal(0m, schedule[0].RemainingPrincipalAfter);
    }

    [Fact]
    public void ScheduleCalculator_InvalidArguments_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.CalculateSchedule(0m, 12, DateTime.Today));
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.CalculateSchedule(-500m, 12, DateTime.Today));
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.CalculateSchedule(1000m, 0, DateTime.Today));
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.CalculateSchedule(1000m, -1, DateTime.Today));
    }

    [Fact]
    public void MultipleLoans_CombinedMonthlyDeduction_SumsRegularAndAdjustedMonths()
    {
        // Store A: MVR 12,000 / 24 months -> 500.00 / month
        // Store B: MVR 8,000 / 24 months -> 333.33 normal, 333.41 final
        var scheduleA = _calculator.CalculateSchedule(12000m, 24, new DateTime(2027, 1, 1));
        var scheduleB = _calculator.CalculateSchedule(8000m, 24, new DateTime(2027, 1, 1));

        // Normal month combined deduction (e.g., month 1)
        decimal normalCombined = scheduleA[0].ScheduledAmount + scheduleB[0].ScheduledAmount;
        Assert.Equal(833.33m, normalCombined);

        // Final month combined deduction (month 24)
        decimal finalCombined = scheduleA[23].ScheduledAmount + scheduleB[23].ScheduledAmount;
        Assert.Equal(833.41m, finalCombined);

        // Total repaid across both schedules
        decimal grandTotal = scheduleA.Sum(x => x.ScheduledAmount) + scheduleB.Sum(x => x.ScheduledAmount);
        Assert.Equal(20000.00m, grandTotal);
    }

    [Fact]
    public void PayrollCalculationEngine_IncludesGoodsLoanDeductionComponent()
    {
        var engine = new PayrollCalculationEngine();
        var periodStart = new DateOnly(2026, 11, 1);
        var periodEnd = new DateOnly(2026, 11, 30);

        // Create an employee payroll input with a GoodsLoan deduction item of 833.33
        var deductions = new List<InputDeductionAgreementItem>
        {
            new(
                InstalmentId: 101,
                AgreementId: 201,
                Reference: "GL-202611-0001",
                Category: "GoodsLoan",
                DueAmount: 833.33m
            )
        };

        var input = new EmployeePayrollInputItem(
            JobId: 101,
            IndividualId: 501,
            EmployeeNumber: "EMP-501",
            EmployeeName: "Ahmed Ali",
            DepartmentName: "Finance",
            PositionTitle: "Accountant",
            GradeCode: "G4",
            StepNumber: 2,
            BasicSalary: 12000m,
            CurrencyTypeId: 1,
            BankName: "BML",
            BankBranch: "Male",
            BankAccountNumber: "7701501000",
            AccountHolderName: "Ahmed Ali",
            JoinedDate: new DateOnly(2020, 1, 1),
            TerminatedDate: null,
            Allowances: new List<InputAllowanceItem>(),
            OvertimeClaims: new List<InputOvertimeItem>(),
            UnpaidLeaves: new List<InputLeaveDeductionItem>(),
            Deductions: deductions,
            Adjustments: new List<InputAdjustmentItem>()
        );

        var outcome = engine.CalculateEmployee(input, periodStart, periodEnd, employeePensionRate: 0.07m, employerPensionRate: 0.07m);

        Assert.Equal(12000.00m, outcome.BaseSalary);
        Assert.Equal(12000.00m, outcome.GrossEarnings);
        Assert.Contains(outcome.Lines, l => l.ComponentCode == "GL_DED" && l.CalculatedAmount == 833.33m);
        Assert.True(outcome.TotalDeductions >= 833.33m);
        Assert.True(outcome.NetSalary <= 12000.00m - 833.33m);
    }
}
