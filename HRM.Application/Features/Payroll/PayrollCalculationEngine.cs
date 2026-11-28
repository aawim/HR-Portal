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
        decimal taxRate = 0.055m,
        PayrollStatutoryPolicy? statutoryPolicy = null
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
        decimal taxRate = 0.055m,
        PayrollStatutoryPolicy? statutoryPolicy = null)
    {
        var policy = input.FrozenPolicy ?? statutoryPolicy ?? PayrollStatutoryPolicy.MaldivesDefault;
        if (input.FrozenPolicy != null || statutoryPolicy != null)
        {
            employeePensionRate = policy.EmployeePensionRate;
            employerPensionRate = policy.EmployerPensionRate;
        }
        var lines = new List<PayrollLineDto>();
        var warnings = new List<string>();
        var errors = new List<string>();
        if (input.ValidationErrors != null) errors.AddRange(input.ValidationErrors);
        var policyError = MaldivesPayrollRules.ValidatePolicy(policy with
        {
            EmployeePensionRate = employeePensionRate, EmployerPensionRate = employerPensionRate
        });
        if (policyError != null) errors.Add(policyError);
        if (!input.NationalityVerified) errors.Add("Verify employee nationality before calculating statutory pension and allowances.");
        if (input.BasicSalary < 0 || input.Allowances.Any(a => a.Amount < 0) ||
            input.OvertimeClaims.Any(o => o.Amount < 0 || o.Minutes < 0) ||
            input.Adjustments.Any(a => a.Amount < 0) || input.Deductions.Any(d => d.DueAmount < 0))
            errors.Add("Salary, allowances, overtime and deduction amounts cannot be negative.");

        if (periodEnd < periodStart)
        {
            errors.Add("Payroll period end date cannot be earlier than its start date.");
            return new(0, 0, 0, 0, 0, lines, warnings, errors);
        }
        if (periodStart.Year != periodEnd.Year || periodStart.Month != periodEnd.Month)
        {
            errors.Add("A regular monthly payroll period must start and end in the same calendar month.");
            return new(0, 0, 0, 0, 0, lines, warnings, errors);
        }

        int daysInMonth = DateTime.DaysInMonth(periodStart.Year, periodStart.Month);
        var employmentStart = input.JoinedDate > periodStart ? input.JoinedDate : periodStart;
        var employmentEnd = input.TerminatedDate.HasValue && input.TerminatedDate.Value < periodEnd
            ? input.TerminatedDate.Value
            : periodEnd;
        if (employmentStart > periodEnd)
        {
            errors.Add($"Employee joined after payroll period ({input.JoinedDate:yyyy-MM-dd} > {periodEnd:yyyy-MM-dd}).");
            return new(0, 0, 0, 0, 0, lines, warnings, errors);
        }
        if (employmentEnd < periodStart || employmentEnd < employmentStart)
        {
            errors.Add($"Employee was not active during payroll period ({periodStart:yyyy-MM-dd} to {periodEnd:yyyy-MM-dd}).");
            return new(0, 0, 0, 0, 0, lines, warnings, errors);
        }
        int workedDays = employmentEnd.DayNumber - employmentStart.DayNumber + 1;

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
            if (allowance.FromDate > employmentEnd || allowance.ToDate < employmentStart) continue;
            decimal allowanceAmount = allowance.Amount;
            if (allowance.IsProratable && daysInMonth > 0)
            {
                var allowanceStart = allowance.FromDate > employmentStart ? allowance.FromDate : employmentStart;
                var allowanceEnd = allowance.ToDate.HasValue && allowance.ToDate.Value < employmentEnd
                    ? allowance.ToDate.Value
                    : employmentEnd;
                var allowanceDays = allowanceEnd < allowanceStart ? 0 : allowanceEnd.DayNumber - allowanceStart.DayNumber + 1;
                allowanceAmount = decimal.Round((allowance.Amount / daysInMonth) * allowanceDays, 2, MidpointRounding.AwayFromZero);
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
        decimal taxableAdditionAdjustments = 0m;
        foreach (var adj in input.Adjustments.Where(x => x.IsAddition))
        {
            // Ramadan allowance is a taxable lump-sum payment (MIRA 601 instructions).
            bool isTaxableAdj = adj.IsTaxable || adj.ComponentCode.Contains("RAMADAN", StringComparison.OrdinalIgnoreCase);
            lines.Add(new PayrollLineDto(
                0,
                0,
                adj.ComponentCode,
                adj.Description,
                SalaryComponentType.Bonus,
                adj.Amount,
                1.0m,
                adj.Amount,
                IsTaxable: isTaxableAdj,
                IsPensionable: false,
                $"AdjId: {adj.AdjustmentId}"
            ));
            totalAdditionAdjustments += adj.Amount;
            if (isTaxableAdj) taxableAdditionAdjustments += adj.Amount;
        }

        // Gross Earnings
        decimal grossEarnings = proratedBasic + totalAllowances + totalOvertime + totalAdditionAdjustments;

        // 4b. Deductions: Attendance Lateness & Absence Deductions (Civil Service Reg Art 149 & 160)
        decimal totalAttendanceBasicDeductions = 0m;
        decimal totalAttendanceServiceDeductions = 0m;
        decimal totalAttendanceOtherDeductions = 0m;

        if (input.AttendanceDeductions != null)
        {
            foreach (var att in input.AttendanceDeductions)
            {
                if (att.DeductionAmount <= 0) continue;

                var compCode = att.DeductionType switch
                {
                    "LateArrival" => "DED_ATT_LATE",
                    "FullDayAbsence" when att.AffectsServiceAllowance => "DED_ATT_ABSENCE_SERV",
                    "FullDayAbsence_ServiceAllowance" => "DED_ATT_ABSENCE_SERV",
                    "FullDayAbsence" => "DED_ATT_ABSENCE_BASIC",
                    "PartialDayAbsence" => "DED_ATT_PARTIAL",
                    "EarlyDeparture" => "DED_ATT_EARLY",
                    _ => "DED_ATT_OTHER"
                };

                var compName = att.DeductionType switch
                {
                    "LateArrival" => $"Late Attendance Deduction ({att.DeductibleMinutes} mins on {att.WorkDate:dd MMM})",
                    "FullDayAbsence" when att.AffectsServiceAllowance => $"Unauthorised Absence - Service Allowance ({att.WorkDate:dd MMM})",
                    "FullDayAbsence" => $"Unauthorised Absence - Basic Salary ({att.WorkDate:dd MMM})",
                    "PartialDayAbsence" => $"Partial-Day Absence ({att.DeductibleMinutes} mins on {att.WorkDate:dd MMM})",
                    "EarlyDeparture" => $"Early Departure ({att.DeductibleMinutes} mins on {att.WorkDate:dd MMM})",
                    _ => $"Attendance Deduction ({att.WorkDate:dd MMM})"
                };

                var compType = att.DeductionType == "LateArrival"
                    ? SalaryComponentType.AttendanceLatenessDeduction
                    : SalaryComponentType.AttendanceAbsenceDeduction;

                lines.Add(new PayrollLineDto(
                    0,
                    0,
                    compCode,
                    compName,
                    compType,
                    att.DeductionAmount,
                    1.0m,
                    att.DeductionAmount,
                    IsTaxable: false,
                    IsPensionable: att.AffectsBasicSalary,
                    SourceReference: $"{att.SourceReference} ({att.PolicyCode})"
                ));

                if (att.AffectsBasicSalary)
                {
                    totalAttendanceBasicDeductions += att.DeductionAmount;
                }
                else if (att.AffectsServiceAllowance)
                {
                    totalAttendanceServiceDeductions += att.DeductionAmount;
                }
                else
                {
                    totalAttendanceOtherDeductions += att.DeductionAmount;
                }
            }
        }

        // 5. Deductions: Unpaid Leaves (computed before MRPS & MIRA EWT so unpaid leave reduces pensionable & taxable base)
        decimal totalUnpaidLeaveDeductions = 0m;
        int leaveDivisor = string.Equals(policy.UnpaidLeaveDivisorMode, "Fixed30", StringComparison.OrdinalIgnoreCase) ? 30 : daysInMonth;
        var leaveDates = new HashSet<DateOnly>();
        foreach (var leave in input.UnpaidLeaves)
        {
            if (leave.DeductionRate < 0 || leave.DeductionRate > 1)
            {
                errors.Add($"Unpaid leave {leave.LeaveId} has an invalid deduction rate.");
                continue;
            }
            var from = leave.FromDate > employmentStart ? leave.FromDate : employmentStart;
            var to = leave.ToDate < employmentEnd ? leave.ToDate : employmentEnd;
            if (to < from) continue;
            int leaveDays = to.DayNumber - from.DayNumber + 1;
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                if (!leaveDates.Add(date)) errors.Add($"Overlapping unpaid leave on {date:yyyy-MM-dd}; correct the source records.");
                if (input.AttendanceDeductions?.Any(a => a.WorkDate == date && a.DeductionAmount > 0) == true)
                    errors.Add($"Attendance and unpaid leave both deduct pay on {date:yyyy-MM-dd}; review before payment.");
            }
            decimal dailyRate = leaveDivisor > 0 ? (basicSalary / leaveDivisor) : 0m;
            decimal deductionAmount = decimal.Round(dailyRate * leaveDays * leave.DeductionRate, 2, MidpointRounding.AwayFromZero);
            if (deductionAmount > 0)
            {
                lines.Add(new PayrollLineDto(
                    0,
                    0,
                    "UNPAID_LEAVE",
                    $"Unpaid Leave ({leave.LeaveTypeName} - {leaveDays} days)",
                    SalaryComponentType.UnpaidLeaveDeduction,
                    dailyRate,
                    leaveDays,
                    deductionAmount,
                    IsTaxable: false,
                    IsPensionable: false,
                    $"LeaveId: {leave.LeaveId}"
                ));
                totalUnpaidLeaveDeductions += deductionAmount;
            }
        }

        // 6. Deductions: MRPS Pension (Maldives Pension Act 8/2009 & MPAO Reg 2021/R-27)
        // Mandatory for eligible Maldivian citizens within configured statutory age bounds (default 16-65).
        bool pensionEligible = input.IsMaldivianCitizen || input.TaxContext?.Profile?.VoluntaryExpatriatePension == true;
        if (pensionEligible && !input.DateOfBirth.HasValue)
            errors.Add("Date of birth is required to verify MRPS age eligibility.");
        if (pensionEligible && input.DateOfBirth.HasValue)
        {
            int age = periodEnd.Year - input.DateOfBirth.Value.Year;
            if (input.DateOfBirth.Value > periodEnd.AddYears(-age)) age--;
            if (age < policy.PensionMinAge || age >= policy.PensionMaxAge)
            {
                pensionEligible = false;
            }
        }

        decimal adjustedBasicSalary = Math.Max(0m, proratedBasic - totalAttendanceBasicDeductions - totalUnpaidLeaveDeductions);
        if (totalAttendanceBasicDeductions + totalUnpaidLeaveDeductions > proratedBasic)
            errors.Add("Attendance and unpaid-leave deductions exceed earned basic salary.");
        decimal pensionBase = pensionEligible ? (adjustedBasicSalary + pensionableAllowances) : 0m;
        decimal employeePension = pensionEligible ? decimal.Round(pensionBase * employeePensionRate, 2, MidpointRounding.AwayFromZero) : 0m;
        decimal employerPension = pensionEligible ? decimal.Round(pensionBase * employerPensionRate, 2, MidpointRounding.AwayFromZero) : 0m;

        if (employeePension > 0)
        {
            lines.Add(new PayrollLineDto(
                0,
                0,
                "PEN_EMP",
                $"Employee Pension ({employeePensionRate:P0})",
                SalaryComponentType.EmployeePension,
                employeePensionRate,
                pensionBase,
                employeePension,
                IsTaxable: false,
                IsPensionable: false,
                "Pension Base: " + pensionBase.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
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
                pensionBase,
                employerPension,
                IsTaxable: false,
                IsPensionable: false,
                "Statutory Employer Contribution"
            ));
        }

        // 7. Deductions: Loan and Agreement Deductions
        decimal totalAgreementDeductions = 0m;
        foreach (var ded in input.Deductions)
        {
            var isGoodsLoan = string.Equals(ded.Category, "GoodsLoan", StringComparison.OrdinalIgnoreCase);
            var compCode = isGoodsLoan ? "GL_DED" : "AGREEMENT_DED";
            var compName = isGoodsLoan ? $"Goods Loan ({ded.Reference})" : $"{ded.Category} ({ded.Reference})";

            lines.Add(new PayrollLineDto(
                0,
                0,
                compCode,
                compName,
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

        // 9. Employee Withholding Tax (MIRA Progressive Income Tax Brackets — Income Tax Act 25/2019)
        // Taxable base = Taxable earnings (net of attendance & unpaid leave deductions) - Employee MRPS Pension
        decimal totalAttendanceDeductions = totalAttendanceBasicDeductions + totalAttendanceServiceDeductions + totalAttendanceOtherDeductions;
        decimal taxableBase = Math.Max(0m,
            (proratedBasic - totalAttendanceBasicDeductions - totalUnpaidLeaveDeductions)
            + (taxableAllowances - totalAttendanceServiceDeductions)
            + totalOvertime
            + taxableAdditionAdjustments
            + (input.TaxContext?.Profile?.MonthlyTaxableNonCashBenefits ?? 0)
            - employeePension);

        var brackets = (policy.TaxBrackets != null && policy.TaxBrackets.Count > 0)
            ? policy.TaxBrackets
            : PayrollStatutoryPolicy.MaldivesDefaultTaxBrackets;

        decimal employeeTax = MaldivesPayrollRules.CalculateEwt(taxableBase, input.TaxContext, brackets, errors, warnings);
        // Keep a zero-tax line too: payslips and year-to-date calculations need the taxable base.
        {
            decimal effectiveRate = taxableBase > 0 ? decimal.Round(employeeTax / taxableBase, 4, MidpointRounding.AwayFromZero) : 0m;
            lines.Add(new PayrollLineDto(
                0,
                0,
                "TAX_WHT",
                "Employee Withholding Tax (MIRA EWT)",
                SalaryComponentType.EmployeeTax,
                effectiveRate,
                taxableBase,
                employeeTax,
                IsTaxable: false,
                IsPensionable: false,
                FormattableString.Invariant($"Taxable Base: {taxableBase:F2}; Non-cash benefits: {input.TaxContext?.Profile?.MonthlyTaxableNonCashBenefits ?? 0:F2}")
            ));
        }

        decimal totalEmployeeDeductions = employeePension + employeeTax + totalUnpaidLeaveDeductions + totalAgreementDeductions + totalDeductionAdjustments + totalAttendanceDeductions;
        decimal netSalary = grossEarnings - totalEmployeeDeductions;

        if (netSalary < 0)
        {
            errors.Add($"Negative net salary calculated ({netSalary:C2}). Gross: {grossEarnings:C2}, Deductions: {totalEmployeeDeductions:C2}.");
        }
        else if (netSalary < (basicSalary * 0.2m) && basicSalary > 0)
        {
            warnings.Add($"Net take-home pay is below 20% of basic salary ({netSalary:C2} / {basicSalary:C2}).");
        }

        decimal fixedAllowances = lines.Where(l => l.ComponentType == SalaryComponentType.FixedAllowance).Sum(l => l.CalculatedAmount);
        if ((!policy.MinimumWageMaldiviansOnly || input.IsMaldivianCitizen) && policy.MinimumWageMonthly > 0 && workedDays == daysInMonth && (basicSalary + fixedAllowances) < policy.MinimumWageMonthly)
        {
            errors.Add($"Fixed monthly remuneration ({basicSalary + fixedAllowances:F2} {policy.DefaultCurrencyCode}) is below the configured minimum wage ({policy.MinimumWageMonthly:F2} {policy.DefaultCurrencyCode}).");
        }

        decimal nonStatutoryDeductions = totalAgreementDeductions + totalDeductionAdjustments;
        if (policy.EmploymentSector == "PrivateSector" && nonStatutoryDeductions > grossEarnings / 3m)
            errors.Add("Non-statutory deductions exceed one third of wages. Review written consent and Employment Act section 55 before payment.");
        if (policy.MaxDeductionPercentage > 0 && grossEarnings > 0 && (nonStatutoryDeductions / grossEarnings) * 100m > policy.MaxDeductionPercentage)
        {
            warnings.Add($"Non-statutory deductions ({nonStatutoryDeductions:F2}) exceed {policy.MaxDeductionPercentage:F0}% of gross earnings ({grossEarnings:F2}).");
        }

        if (policy.RequireBankAccountForPayment && netSalary > 0 && string.IsNullOrWhiteSpace(input.BankAccountNumber))
        {
            warnings.Add("Missing bank account number. Payment batch preparation will be blocked until verified bank details are added.");
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

    public static decimal CalculateProgressiveTax(decimal taxableIncome, IReadOnlyList<HRM.Application.Contracts.Pages.PayrollTaxBracketDto> brackets)
    {
        if (taxableIncome <= 0 || brackets.Count == 0) return 0m;

        decimal totalTax = 0m;
        foreach (var bracket in brackets.OrderBy(b => b.MinAmount))
        {
            if (taxableIncome <= bracket.MinAmount) break;
            decimal upper = bracket.MaxAmount.HasValue ? Math.Min(taxableIncome, bracket.MaxAmount.Value) : taxableIncome;
            decimal slice = upper - bracket.MinAmount;
            if (slice > 0 && bracket.RatePercentage > 0)
            {
                totalTax += slice * (bracket.RatePercentage / 100m);
            }
        }
        return decimal.Round(totalTax, 2, MidpointRounding.AwayFromZero);
    }
}
