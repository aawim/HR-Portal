using System;
using System.Collections.Generic;
using System.Linq;
using HRM.Application.Contracts.Pages;

namespace HRM.Application.Features.Payroll;

public static class MaldivesPayrollRules
{
    public static string? ValidateBrackets(IReadOnlyList<PayrollTaxBracketDto> brackets)
    {
        if (brackets.Count == 0) return "At least one tax bracket is required.";
        decimal next = 0;
        var ordered = brackets.OrderBy(b => b.MinAmount).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            var b = ordered[i];
            if (b.MinAmount != next || b.RatePercentage < 0 || b.RatePercentage > 100 ||
                (b.MaxAmount.HasValue && b.MaxAmount <= b.MinAmount) ||
                (i < ordered.Count - 1 && !b.MaxAmount.HasValue))
                return "Tax brackets must cover income from zero without gaps, overlaps or invalid rates.";
            next = b.MaxAmount ?? next;
        }
        return ordered[^1].MaxAmount.HasValue ? "The final tax bracket must have no upper limit." : null;
    }

    public static string? ValidatePolicy(PayrollStatutoryPolicy policy)
    {
        if (policy.EmploymentSector is not ("CivilService" or "PrivateSector"))
            return "Select Civil service or Private sector for payroll.";
        if (policy.EmployeePensionRate < 0 || policy.EmployeePensionRate > .07m ||
            policy.EmployerPensionRate < .07m || policy.EmployeePensionRate + policy.EmployerPensionRate < .14m)
            return "MRPS requires at least 14% combined, at least 7% employer-funded, and no more than 7% deducted from the employee.";
        if (policy.PensionMinAge != 16 || policy.PensionMaxAge != 65)
            return "Mandatory MRPS age bounds must be 16 to below 65.";
        if (policy.NormalOvertimeMultiplier < 1.25m || policy.HolidayOvertimeMultiplier < 1.50m)
            return "Overtime multipliers must be at least 1.25 on ordinary days and 1.50 on Fridays/public holidays.";
        if (policy.UnpaidLeaveDivisorMode is not ("Fixed30" or "CalendarDays"))
            return "Select a supported unpaid-leave divisor.";
        if (policy.DefaultCurrencyCode != "MVR")
            return "Automatic statutory payroll currently supports MVR only. Foreign-currency payroll needs verified conversion rates and return currency handling.";
        return ValidateBrackets(policy.TaxBrackets ?? PayrollStatutoryPolicy.MaldivesDefaultTaxBrackets);
    }

    // Income Tax Regulation ss.41-42. History is frozen with the payroll input, not read during calculation.
    public static decimal CalculateEwt(decimal taxable, PayrollTaxContext? context,
        IReadOnlyList<PayrollTaxBracketDto> brackets, List<string> errors, List<string> warnings)
    {
        var profile = context?.Profile;
        if (profile?.TaxTreatment == "MiraDirection")
        {
            if (string.IsNullOrWhiteSpace(profile.AuthorityReference) || profile.DirectedTaxBrackets == null ||
                ValidateBrackets(profile.DirectedTaxBrackets) != null)
            {
                errors.Add("MIRA-directed EWT requires the authority reference and valid directed brackets.");
                return 0;
            }
            return PayrollCalculationEngine.CalculateProgressiveTax(taxable, profile.DirectedTaxBrackets);
        }
        if (profile?.TaxTreatment == "Secondary")
        {
            if (string.IsNullOrWhiteSpace(profile.AuthorityReference)) errors.Add("Secondary-employer EWT requires a MIRA 916 election or MIRA determination reference.");
            return PayrollCalculationEngine.CalculateProgressiveTax(taxable,
                [new(0, 150000, 8), new(150000, 200000, 12), new(200000, null, 15)]);
        }
        if (profile != null && profile.TaxTreatment != "Primary") errors.Add("Unknown employee EWT treatment.");
        decimal monthly = PayrollCalculationEngine.CalculateProgressiveTax(taxable, brackets);
        if (profile?.RegularMonthlyTaxableRemuneration is not decimal regular)
        {
            if (taxable > 60000) warnings.Add("Confirm the employee's regular/estimated annual remuneration and MIRA election before relying on monthly EWT; section 42 may apply.");
            return monthly;
        }
        if (regular > 60000) return monthly;
        if (context?.HistoryComplete != true)
        {
            errors.Add("Section 42 EWT requires complete tax-year history or certified opening balances through the previous month.");
            return 0;
        }
        var history = context.PreviousMonths ?? [];
        decimal priorTaxable = history.Sum(m => m.TaxableRemuneration) + profile.OpeningTaxableRemuneration;
        decimal priorTax = history.Sum(m => m.TaxWithheld) + profile.OpeningTaxWithheld;
        decimal yearTaxable = priorTaxable + taxable;
        decimal factor = profile.AccountingPeriodDays / 365m;
        var annualBrackets = PayrollStatutoryPolicy.MaldivesDefaultTaxBrackets
            .Select(b => new PayrollTaxBracketDto(b.MinAmount * 12 * factor, b.MaxAmount * 12 * factor, b.RatePercentage)).ToList();
        decimal cumulative = Math.Max(0, PayrollCalculationEngine.CalculateProgressiveTax(yearTaxable, annualBrackets) - priorTax);
        bool ceased = profile.ExemptionCeased || history.Any(m => m.TaxableRemuneration > 60000 &&
            history.Any(n => n.Month == m.Month + 1 && n.TaxableRemuneration > 60000));
        if (yearTaxable > 720000 * factor) return Math.Max(ceased ? monthly : 0, cumulative);
        if (ceased) return monthly;
        if (profile.EstimatedAnnualTaxableRemuneration is not decimal estimate)
        {
            errors.Add("Section 42 EWT requires an approved estimate of annual taxable remuneration.");
            return 0;
        }
        return estimate <= 720000 * factor ? 0 : monthly;
    }
}
