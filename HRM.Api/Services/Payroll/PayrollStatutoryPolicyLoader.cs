using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HRM.Application.Contracts.Pages;
using HRM.Application.Features.Payroll;
using HRM.Models;
using HRM.Models.Payroll;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Payroll;

internal static class PayrollStatutoryPolicyLoader
{
    public const string StatutoryRuleCode = "STATUTORY_POLICY";

    public static async Task<PayrollStatutoryPolicy> LoadAsync(HrmTeContext db, int organisationId, CancellationToken ct = default)
    {
        var rules = await db.CalculationRules
            .AsNoTracking()
            .Where(r => r.OrganisationId == organisationId && r.IsActive)
            .ToListAsync(ct);

        return FromRules(rules);
    }

    public static PayrollStatutoryPolicy FromRules(IReadOnlyList<CalculationRule> rules)
    {
        var eeRule = rules.FirstOrDefault(r => r.RuleCode == "PENSION_EE");
        var erRule = rules.FirstOrDefault(r => r.RuleCode == "PENSION_ER");
        var otRule = rules.FirstOrDefault(r => r.RuleCode == "OT_STANDARD");
        var statutoryRule = rules.FirstOrDefault(r => r.RuleCode == StatutoryRuleCode);

        decimal eeRate = eeRule != null && eeRule.PercentageValue >= 0 ? eeRule.PercentageValue / 100m : 0.07m;
        decimal erRate = erRule != null && erRule.PercentageValue >= 0 ? erRule.PercentageValue / 100m : 0.07m;
        decimal normalOt = otRule != null && otRule.PercentageValue > 0 ? otRule.PercentageValue : 1.25m;

        var defaults = PayrollStatutoryPolicy.MaldivesDefault;
        if (string.IsNullOrWhiteSpace(statutoryRule?.ConfigurationJson))
        {
            return defaults with
            {
                EmployeePensionRate = eeRate,
                EmployerPensionRate = erRate,
                NormalOvertimeMultiplier = normalOt
            };
        }

        try
        {
            var stored = JsonSerializer.Deserialize<StoredStatutoryConfig>(statutoryRule.ConfigurationJson);
            if (stored == null)
            {
                return defaults with
                {
                    EmployeePensionRate = eeRate,
                    EmployerPensionRate = erRate,
                    NormalOvertimeMultiplier = normalOt
                };
            }

            var brackets = stored.TaxBrackets is { Count: > 0 }
                ? stored.TaxBrackets.OrderBy(b => b.MinAmount).ToList()
                : PayrollStatutoryPolicy.MaldivesDefaultTaxBrackets.ToList();

            return new PayrollStatutoryPolicy(
                EmployeePensionRate: eeRate,
                EmployerPensionRate: erRate,
                AllowancesArePensionable: stored.AllowancesArePensionable,
                PensionMaldiviansOnly: stored.PensionMaldiviansOnly,
                PensionMinAge: stored.PensionMinAge is >= 14 and <= 100 ? stored.PensionMinAge : 16,
                PensionMaxAge: stored.PensionMaxAge is >= 16 and <= 120 ? stored.PensionMaxAge : 65,
                NormalOvertimeMultiplier: normalOt,
                HolidayOvertimeMultiplier: stored.HolidayOvertimeMultiplier >= 1.0m ? stored.HolidayOvertimeMultiplier : 1.50m,
                OvertimeHourlyDivisor: stored.OvertimeHourlyDivisor > 0 ? stored.OvertimeHourlyDivisor : 180m,
                MinimumWageMonthly: stored.MinimumWageMonthly >= 0 ? stored.MinimumWageMonthly : 7000m,
                MaxDeductionPercentage: stored.MaxDeductionPercentage >= 0 ? stored.MaxDeductionPercentage : 40m,
                RamadanAllowanceAmount: stored.RamadanAllowanceAmount >= 0 ? stored.RamadanAllowanceAmount : 3000m,
                UnpaidLeaveDivisorMode: string.Equals(stored.UnpaidLeaveDivisorMode, "Fixed30", StringComparison.OrdinalIgnoreCase) ? "Fixed30" : "CalendarDays",
                RequireBankAccountForPayment: stored.RequireBankAccountForPayment,
                DefaultCurrencyCode: string.IsNullOrWhiteSpace(stored.DefaultCurrencyCode) ? "MVR" : stored.DefaultCurrencyCode.Trim().ToUpperInvariant(),
                TaxBrackets: brackets
            );
        }
        catch
        {
            return defaults with
            {
                EmployeePensionRate = eeRate,
                EmployerPensionRate = erRate,
                NormalOvertimeMultiplier = normalOt
            };
        }
    }

    public sealed class StoredStatutoryConfig
    {
        public decimal HolidayOvertimeMultiplier { get; set; } = 1.50m;
        public decimal OvertimeHourlyDivisor { get; set; } = 180m;
        public bool AllowancesArePensionable { get; set; } = false;
        public bool PensionMaldiviansOnly { get; set; } = true;
        public int PensionMinAge { get; set; } = 16;
        public int PensionMaxAge { get; set; } = 65;
        public decimal MinimumWageMonthly { get; set; } = 7000m;
        public decimal MaxDeductionPercentage { get; set; } = 40m;
        public decimal RamadanAllowanceAmount { get; set; } = 3000m;
        public string UnpaidLeaveDivisorMode { get; set; } = "CalendarDays";
        public bool RequireBankAccountForPayment { get; set; } = true;
        public string DefaultCurrencyCode { get; set; } = "MVR";
        public List<PayrollTaxBracketDto>? TaxBrackets { get; set; }
    }
}
