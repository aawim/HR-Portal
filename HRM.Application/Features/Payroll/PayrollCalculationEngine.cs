using System;
using System.Collections.Generic;
using System.Linq;
using HRM.Models.Payroll;

namespace HRM.Application.Features.Payroll;

public interface IPayrollCalculationEngine
{
    PayrollCalculationOutcome CalculateEmployee(
        EmployeePayrollInputItem input,
        DateOnly periodStart,
        DateOnly periodEnd,
        decimal employeePensionRate = 0.07m,
        decimal employerPensionRate = 0.07m,
        decimal taxThreshold = 60000m,
        decimal taxRate = 0.05m
    );
}

public sealed record PayrollCalculationOutcome(
    decimal BaseSalary,
    decimal GrossEarnings,
    decimal TotalDeductions,
    decimal EmployerContributions,
    decimal NetSalary,
    List<PayrollLineDto> Lines,
    List<string> Warnings,
    List<string> Errors
);

public sealed class PayrollCalculationEngine : IPayrollCalculationEngine
{
    public PayrollCalculationOutcome CalculateEmployee(
        EmployeePayrollInputItem input,
        DateOnly periodStart,
        DateOnly periodEnd,
        decimal employeePensionRate = 0.07m,
        decimal employerPensionRate = 0.07m,
        decimal taxThreshold = 60000m,
        decimal taxRate = 0.05m)
    {
        var lines = new List<PayrollLineDto>();
        var warnings = new List<string>();
        var errors = new List<string>();

        int daysInMonth = DateTime.DaysInMonth(periodStart.Year, periodStart.Month);
        int workedDays = daysInMonth;

        // Proration check for mid-month join or termination
        if (input.JoinedDate > periodStart)
        {
            if (input.JoinedDate > periodEnd)
            {
                errors.Add($"Employee joined after payroll period ({input.JoinedDate:yyyy-MM-dd} > {periodEnd:yyyy-MM-dd}).");
                return new(0, 0, 0, 0, 0, lines, warnings, errors);
            }
            workedDays = (periodEnd.Day - input.JoinedDate.Day) + 1;
        }

        if (input.TerminatedDate.HasValue && input.TerminatedDate.Value < periodEnd)
        {
            if (input.TerminatedDate.Value < periodStart)
            {
                errors.Add($"Employee was terminated before payroll period ({input.TerminatedDate.Value:yyyy-MM-dd} < {periodStart:yyyy-MM-dd}).");
                return new(0, 0, 0, 0, 0, lines, warnings, errors);
            }
            int terminateDay = input.TerminatedDate.Value.Day;
            int startDay = input.JoinedDate > periodStart ? input.JoinedDate.Day : 1;
            workedDays = (terminateDay - startDay) + 1;
        }

        if (workedDays < 0) workedDays = 0;
        if (workedDays > daysInMonth) workedDays = daysInMonth;

        // 1. Basic Salary
        decimal basicSalary = input.BasicSalary;
        decimal proratedBasic = basicSalary;
        if (workedDays < daysInMonth && daysInMonth > 0)
        {
            proratedBasic = decimal.Round((basicSalary / daysInMonth) * workedDays, 2, MidpointRounding.AwayFromZero);
        }

        lines.Add(new PayrollLineDto(
            0,
            0,
            "BASIC",
            "Basic Salary",
            SalaryComponentType.BasicSalary,
            basicSalary,
            workedDays == daysInMonth ? 1.0m : (decimal)workedDays / daysInMonth,
            proratedBasic,
            IsTaxable: true,
            IsPensionable: true,
            SourceReference: $"Days: {workedDays}/{daysInMonth}"
        ));

        // 2. Allowances
        decimal totalAllowances = 0m;
        decimal pensionableAllowances = 0m;
        decimal taxableAllowances = 0m;

        foreach (var allowance in input.Allowances)
        {
            decimal allowanceAmount = allowance.Amount;
            if (allowance.IsProratable && workedDays < daysInMonth && daysInMonth > 0)
            {
                allowanceAmount = decimal.Round((allowance.Amount / daysInMonth) * workedDays, 2, MidpointRounding.AwayFromZero);
            }

            if (allowanceAmount > 0)
            {
                lines.Add(new PayrollLineDto(
                    0,
                    0,
                    allowance.ComponentCode,
                    allowance.Name,
                    allowance.Type,
                    allowance.Amount,
                    1.0m,
                    allowanceAmount,
                    allowance.IsTaxable,
                    allowance.IsPensionable,
                    $"AllowanceId: {allowance.AllowanceId}"
                ));

                totalAllowances += allowanceAmount;
                if (allowance.IsPensionable) pensionableAllowances += allowanceAmount;
                if (allowance.IsTaxable) taxableAllowances += allowanceAmount;
            }
        }

        // 3. Overtime
        decimal totalOvertime = 0m;
        foreach (var ot in input.OvertimeClaims)
        {
            lines.Add(new PayrollLineDto(
                0,
                0,
                "OVERTIME",
                $"Overtime ({ot.Minutes} mins)",
                SalaryComponentType.Overtime,
                ot.HourlyRate,
                decimal.Round((decimal)ot.Minutes / 60m, 2, MidpointRounding.AwayFromZero),
                ot.Amount,
                IsTaxable: true,
                IsPensionable: false,
                ot.RequestReference
            ));
            totalOvertime += ot.Amount;
        }

        // 4. Adjustments (Additions)
        decimal totalAdditionAdjustments = 0m;
        foreach (var adj in input.Adjustments.Where(x => x.IsAddition))
        {
            lines.Add(new PayrollLineDto(
                0,
                0,
                adj.ComponentCode,
                adj.Description,
                SalaryComponentType.Bonus,
                adj.Amount,
                1.0m,
                adj.Amount,
                IsTaxable: true,
                IsPensionable: false,
                $"AdjId: {adj.AdjustmentId}"
            ));
            totalAdditionAdjustments += adj.Amount;
        }

        // Gross Earnings
        decimal grossEarnings = proratedBasic + totalAllowances + totalOvertime + totalAdditionAdjustments;

        // 5. Deductions: Pension
        decimal pensionBase = proratedBasic + pensionableAllowances;
        decimal employeePension = decimal.Round(pensionBase * employeePensionRate, 2, MidpointRounding.AwayFromZero);
        decimal employerPension = decimal.Round(pensionBase * employerPensionRate, 2, MidpointRounding.AwayFromZero);

        if (employeePension > 0)
        {
            lines.Add(new PayrollLineDto(
                0,
                0,
                "PEN_EMP",
                $"Employee Pension ({employeePensionRate:P0})",
                SalaryComponentType.EmployeePension,
                employeePensionRate,
                1.0m,
                employeePension,
                IsTaxable: false,
                IsPensionable: false,
                "Pension Base: " + pensionBase.ToString("F2")
            ));
        }

        if (employerPension > 0)
        {
            lines.Add(new PayrollLineDto(
                0,
                0,
                "PEN_EMPR",
                $"Employer Pension ({employerPensionRate:P0})",
                SalaryComponentType.EmployerPension,
                employerPensionRate,
                1.0m,
                employerPension,
                IsTaxable: false,
                IsPensionable: false,
                "Statutory Employer Contribution"
            ));
        }

        // 6. Deductions: Unpaid Leaves
        decimal totalUnpaidLeaveDeductions = 0m;
        foreach (var leave in input.UnpaidLeaves)
        {
            decimal dailyRate = daysInMonth > 0 ? (basicSalary / daysInMonth) : 0m;
            decimal deductionAmount = decimal.Round(dailyRate * leave.CalendarDays * leave.DeductionRate, 2, MidpointRounding.AwayFromZero);
            if (deductionAmount > 0)
            {
                lines.Add(new PayrollLineDto(
                    0,
                    0,
                    "UNPAID_LEAVE",
                    $"Unpaid Leave ({leave.LeaveTypeName} - {leave.CalendarDays} days)",
                    SalaryComponentType.UnpaidLeaveDeduction,
                    dailyRate,
                    leave.CalendarDays,
                    deductionAmount,
                    IsTaxable: false,
                    IsPensionable: false,
                    $"LeaveId: {leave.LeaveId}"
                ));
                totalUnpaidLeaveDeductions += deductionAmount;
            }
        }

        // 7. Deductions: Loan and Agreement Deductions
        decimal totalAgreementDeductions = 0m;
        foreach (var ded in input.Deductions)
        {
            lines.Add(new PayrollLineDto(
                0,
                0,
                "AGREEMENT_DED",
                $"{ded.Category} ({ded.Reference})",
                SalaryComponentType.LoanDeduction,
                ded.DueAmount,
                1.0m,
                ded.DueAmount,
                IsTaxable: false,
                IsPensionable: false,
                $"InstalmentId: {ded.InstalmentId}"
            ));
            totalAgreementDeductions += ded.DueAmount;
        }

        // 8. Adjustments (Deductions)
        decimal totalDeductionAdjustments = 0m;
        foreach (var adj in input.Adjustments.Where(x => !x.IsAddition))
        {
            lines.Add(new PayrollLineDto(
                0,
                0,
                adj.ComponentCode,
                adj.Description,
                SalaryComponentType.OtherDeduction,
                adj.Amount,
                1.0m,
                adj.Amount,
                IsTaxable: false,
                IsPensionable: false,
                $"AdjId: {adj.AdjustmentId}"
            ));
            totalDeductionAdjustments += adj.Amount;
        }

        // 9. Withholding Tax (Income Tax)
        // Taxable base = Taxable earnings - Employee Pension
        decimal taxableBase = proratedBasic + taxableAllowances + totalOvertime + totalAdditionAdjustments - employeePension;
        decimal employeeTax = 0m;
        if (taxableBase > taxThreshold)
        {
            decimal taxableExcess = taxableBase - taxThreshold;
            employeeTax = decimal.Round(taxableExcess * taxRate, 2, MidpointRounding.AwayFromZero);
            lines.Add(new PayrollLineDto(
                0,
                0,
                "TAX_WHT",
                $"Withholding Tax ({taxRate:P0})",
                SalaryComponentType.EmployeeTax,
                taxRate,
                1.0m,
                employeeTax,
                IsTaxable: false,
                IsPensionable: false,
                $"Taxable Excess: {taxableExcess:F2}"
            ));
        }

        decimal totalEmployeeDeductions = employeePension + employeeTax + totalUnpaidLeaveDeductions + totalAgreementDeductions + totalDeductionAdjustments;
        decimal netSalary = grossEarnings - totalEmployeeDeductions;

        if (netSalary < 0)
        {
            errors.Add($"Negative net salary calculated ({netSalary:C2}). Gross: {grossEarnings:C2}, Deductions: {totalEmployeeDeductions:C2}.");
        }
        else if (netSalary < (basicSalary * 0.2m) && basicSalary > 0)
        {
            warnings.Add($"Net take-home pay is below 20% of basic salary ({netSalary:C2} / {basicSalary:C2}).");
        }

        return new PayrollCalculationOutcome(
            BaseSalary: proratedBasic,
            GrossEarnings: grossEarnings,
            TotalDeductions: totalEmployeeDeductions,
            EmployerContributions: employerPension,
            NetSalary: netSalary,
            Lines: lines,
            Warnings: warnings,
            Errors: errors
        );
    }
}
