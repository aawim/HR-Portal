using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HRM.Application.Features.Payroll;
using HRM.Models.Payroll;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class PayrollPerformanceTests
{
    private static EmployeePayrollInputItem GenerateSyntheticEmployee(int index)
    {
        var periodStart = new DateOnly(2026, 11, 1);
        return new EmployeePayrollInputItem(
            JobId: 1000 + index,
            IndividualId: 2000 + index,
            EmployeeNumber: $"EMP-{index:D6}",
            EmployeeName: $"Staff Member {index}",
            DepartmentName: index % 5 == 0 ? "Engineering" : (index % 3 == 0 ? "Finance" : "Operations"),
            PositionTitle: "Specialist Officer",
            GradeCode: $"G{(index % 8) + 1}",
            StepNumber: (index % 5) + 1,
            BasicSalary: 15000m + ((index % 10) * 1000m),
            CurrencyTypeId: 1,
            BankName: "Bank of Maldives",
            BankBranch: "Male",
            BankAccountNumber: $"7701{index:D8}",
            AccountHolderName: $"Staff Member {index}",
            JoinedDate: new DateOnly(2022, 1, 1),
            TerminatedDate: null,
            Allowances: new List<InputAllowanceItem>
            {
                new(1, "DUTY_ALLW", "Duty Allowance", SalaryComponentType.DutyAllowance, 3000m, true, true, false, periodStart, null),
                new(2, "PHONE_ALLW", "Phone Allowance", SalaryComponentType.FixedAllowance, 500m, true, false, false, periodStart, null)
            },
            OvertimeClaims: index % 4 == 0 ? new List<InputOvertimeItem>
            {
                new(5000 + index, $"OT-{index}", periodStart.AddDays(10), 180, 150m, 450m)
            } : new List<InputOvertimeItem>(),
            UnpaidLeaves: index % 10 == 0 ? new List<InputLeaveDeductionItem>
            {
                new(6000 + index, "Unpaid Leave", periodStart.AddDays(5), periodStart.AddDays(6), 2, 1.0m)
            } : new List<InputLeaveDeductionItem>(),
            Deductions: index % 7 == 0 ? new List<InputDeductionAgreementItem>
            {
                new(7000 + index, 8000 + index, $"AGR-{index}", "Salary Advance", 1000m)
            } : new List<InputDeductionAgreementItem>(),
            Adjustments: new List<InputAdjustmentItem>()
        );
    }

    [Fact]
    public void Benchmark_Calculates_100_Employees_Under_50_Milliseconds()
    {
        var engine = new PayrollCalculationEngine();
        var periodStart = new DateOnly(2026, 11, 1);
        var periodEnd = new DateOnly(2026, 11, 30);
        var employees = Enumerable.Range(1, 100).Select(GenerateSyntheticEmployee).ToList();

        var sw = Stopwatch.StartNew();
        foreach (var emp in employees)
        {
            var outcome = engine.CalculateEmployee(emp, periodStart, periodEnd);
            Assert.True(outcome.NetSalary > 0);
        }
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 50, $"100 employees took {sw.ElapsedMilliseconds}ms (Threshold: 50ms)");
    }

    [Fact]
    public void Benchmark_Calculates_1000_Employees_Under_300_Milliseconds()
    {
        var engine = new PayrollCalculationEngine();
        var periodStart = new DateOnly(2026, 11, 1);
        var periodEnd = new DateOnly(2026, 11, 30);
        var employees = Enumerable.Range(1, 1000).Select(GenerateSyntheticEmployee).ToList();

        var sw = Stopwatch.StartNew();
        foreach (var emp in employees)
        {
            var outcome = engine.CalculateEmployee(emp, periodStart, periodEnd);
            Assert.True(outcome.NetSalary > 0);
        }
        sw.Stop();

        double throughput = 1000.0 / (sw.ElapsedMilliseconds / 1000.0);
        Assert.True(sw.ElapsedMilliseconds < 300, $"1,000 employees took {sw.ElapsedMilliseconds}ms (Throughput: {throughput:F0} emp/sec)");
    }

    [Fact]
    public void Benchmark_Calculates_10000_Employees_Under_2500_Milliseconds()
    {
        var engine = new PayrollCalculationEngine();
        var periodStart = new DateOnly(2026, 11, 1);
        var periodEnd = new DateOnly(2026, 11, 30);
        var employees = Enumerable.Range(1, 10000).Select(GenerateSyntheticEmployee).ToList();

        var sw = Stopwatch.StartNew();
        Parallel.ForEach(employees, emp =>
        {
            var outcome = engine.CalculateEmployee(emp, periodStart, periodEnd);
            Assert.True(outcome.NetSalary > 0);
        });
        sw.Stop();

        double throughput = 10000.0 / (sw.ElapsedMilliseconds / 1000.0);
        Assert.True(sw.ElapsedMilliseconds < 2500, $"10,000 employees took {sw.ElapsedMilliseconds}ms in parallel (Throughput: {throughput:F0} emp/sec)");
    }

    [Fact]
    public void Benchmark_MultiOrganisation_ConcurrentPayrollRuns_ProcessDeterministically()
    {
        var engine = new PayrollCalculationEngine();
        var periodStart = new DateOnly(2026, 11, 1);
        var periodEnd = new DateOnly(2026, 11, 30);

        // 5 organisations processing 500 employees each concurrently
        var orgs = Enumerable.Range(1, 5).ToList();

        Parallel.ForEach(orgs, orgId =>
        {
            var employees = Enumerable.Range(1, 500).Select(i => GenerateSyntheticEmployee(orgId * 1000 + i)).ToList();
            foreach (var emp in employees)
            {
                var outcome = engine.CalculateEmployee(emp, periodStart, periodEnd);
                Assert.True(outcome.NetSalary > 0);
                Assert.Equal(outcome.GrossEarnings - outcome.TotalDeductions, outcome.NetSalary);
            }
        });
    }
}
